using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenCvSharp;
using OpenCvSharp.CPlusPlus;

namespace KiritoriMage
{
    public class FoldDetectException : Exception
    {
        public FoldDetectException() { }
        public FoldDetectException(string message) : base(message) { }
        public FoldDetectException(string message, Exception inner) : base(message, inner) { }
    }

    public class FoldDetectUtil
    {
        private const int BlurWidthDivider = 200;
        private const int BestMatchThreshold = 0;
        private const double FoldWidthMean = 0.013;
        private const double FoldWidthStddev = 0.005;
        private const double LeftRightRatioMean = 0.5;
        private const double LeftRightRatioStddev = 0.05;

        private static IEnumerable<IEnumerable<T>> getCombinations<T>(IEnumerable<T> items, int count)
        {
            if (count == 1)
            {
                return items.Select(item => new T[] { item });
            }

            var list = items.ToList();
            return list.SelectMany(item =>
            {
                // get items after item
                var remains = list.SkipWhile(x => !x.Equals(item)).SkipWhile(x => x.Equals(item));
                return getCombinations(remains, count - 1).Select(x => (new T[] { item }).Concat(x));
            });
        }

        // TODO: supports left-to-right image
        public static int[] DetectFolds(string backFilename, out double score)
        {
            score = -1;

            var matSrc = new Mat(backFilename);

            int blur = matSrc.Width / BlurWidthDivider;
            if (blur % 2 == 0)
                blur++;

            var mat = matSrc.CvtColor(ColorConversion.BgrToGray)
                            .MedianBlur(blur)
                            .AdaptiveThreshold(255, AdaptiveThresholdType.MeanC, ThresholdType.Binary, blur, 1);
            mat = ~mat;

            Cv2.Resize(mat, mat, new Size(matSrc.Width, 1), interpolation: Interpolation.Area);
            mat = mat.MedianBlur(blur).Threshold(0, 255, OpenCvSharp.ThresholdType.Otsu);
            Cv2.Resize(mat, mat, new Size(matSrc.Width, matSrc.Height), interpolation: Interpolation.Cubic);
            
            var rects = mat
               .FindContoursAsArray(ContourRetrieval.External, ContourChain.ApproxSimple)
               .Select(x => Cv2.BoundingRect(x))
               .OrderBy(x => x.X)
               .ToArray();

            if (rects.Length < 4)
                throw new FoldDetectException(string.Format("Too few folds: {0}", rects.Length));
            
            var combinations = getCombinations(rects, 4).ToArray();

            var bestMatchRectSet = combinations.Select(_rectCands =>
            {
                var rectCands = _rectCands.OrderBy(x => x.X).ToArray();

                var dataMat = new CvMat(1, 10, MatrixType.F32C1);

                var imageWidthD = (double)matSrc.Width;
                var ratios = rectCands.Select(r => new { 
                    Left = r.X / imageWidthD, 
                    Right = (r.X + r.Width) / imageWidthD,
                    Width = r.Width / imageWidthD }).ToArray();

                // TODO: zero division check
                double leftThin = ratios[0].Left;
                double rightThin = 1 - ratios[3].Right;
                double thinRatio = leftThin / (leftThin + rightThin);

                double leftFat = ratios[1].Left - ratios[0].Right;
                double rightFat = ratios[3].Left - ratios[2].Right;
                double fatRatio = leftFat / (leftFat + rightFat);

                var widthDist = new MathNet.Numerics.Distributions.Normal(FoldWidthMean, FoldWidthStddev);
                var widthLikes = ratios.Select(r => widthDist.DensityLn(r.Width));

                var thinFatDist = new MathNet.Numerics.Distributions.Normal(LeftRightRatioMean, LeftRightRatioStddev);
                var pThin = thinFatDist.DensityLn(thinRatio);
                var pFat = thinFatDist.DensityLn(fatRatio);

                var likes = widthLikes.Concat(new double[] { pThin, pFat });
                var like = likes.Sum();

                return new { Like = like, Rects = rectCands };
            }).OrderByDescending(x => x.Like).First();
            
            if (bestMatchRectSet.Like < BestMatchThreshold)
            {
                throw new FoldDetectException(string.Format("Matched rect is not found (score: {0})", bestMatchRectSet.Like));
            }

            int fold0 = bestMatchRectSet.Rects[0].Right;
            int fold1 = bestMatchRectSet.Rects[1].Right;
            int fold2 = bestMatchRectSet.Rects[3].Right;

            score = bestMatchRectSet.Like;
            return new int[] { fold0, fold1, fold2 };
        }
    }
}
