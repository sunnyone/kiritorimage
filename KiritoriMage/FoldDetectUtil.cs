/*
 * Copyright (c) 2012 Yoichi Imai, All rights reserved.
 * 
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 * 
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
 * NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
 * LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
 * OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
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
        private const double FoldWidthThreshold = 0.03;
        private const double PermissiveRatioDistance = 0.1;

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

            var rectSets = combinations.Select(_rectCands =>
            {
                var rectCands = _rectCands.OrderBy(x => x.X).ToArray();

                var imageWidth = (double)matSrc.Width;
                var widthRatios = rectCands.Select(r => new { 
                    Left = r.X / imageWidth, 
                    Right = (r.X + r.Width) / imageWidth,
                    Width = r.Width / imageWidth }).ToArray();

                // TODO: zero division check
                double leftThin = widthRatios[0].Left;
                double rightThin = 1 - widthRatios[3].Right;
                double thinRatio = leftThin / (leftThin + rightThin);

                double leftFat = widthRatios[1].Left - widthRatios[0].Right;
                double rightFat = widthRatios[3].Left - widthRatios[2].Right;
                double fatRatio = leftFat / (leftFat + rightFat);

                double thinFatDistance = Math.Sqrt(Math.Pow(fatRatio - 0.5, 2) + Math.Pow(thinRatio - 0.5, 2));
                return new { Rects = rectCands, WidthRatios = widthRatios, ThinFatDistance = thinFatDistance, ThinRatio = thinRatio, FatRatio = fatRatio, };
            });

            rectSets = rectSets.Where(r => r.WidthRatios.All(x => x.Width < FoldWidthThreshold)).ToArray();
            if (!rectSets.Any())
            {
                throw new FoldDetectException("All fold rects are too fat.");
            }

            var bestMatchRectSet = rectSets.OrderBy(x => x.ThinFatDistance).First();
            if (bestMatchRectSet.ThinFatDistance > PermissiveRatioDistance)
            {
                throw new FoldDetectException(string.Format("Matched rect is not found (score: {0})", bestMatchRectSet.ThinFatDistance));
            }
            score = bestMatchRectSet.ThinFatDistance;

            int fold0 = bestMatchRectSet.Rects[0].Right;
            int fold1 = bestMatchRectSet.Rects[1].Right;
            int fold2 = bestMatchRectSet.Rects[3].Right;
            return new int[] { fold0, fold1, fold2 };
        }
    }
}
