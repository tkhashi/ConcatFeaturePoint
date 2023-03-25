﻿using System.Diagnostics;
using System.Reactive.Linq;
using OpenCvSharp;

namespace ConcatImages;

public class ConcatService
{
    public void MatchAkaza(string videoPath)
    {
        var cap = VideoCapture.FromFile(videoPath);
        using var akaze = AKAZE.Create();
        using var matcher = new BFMatcher(NormTypes.Hamming);
        Observable.Range(0, cap.FrameCount - 1)
            .Select(i =>
            {
                cap.PosMsec = i * 500;
                return cap.RetrieveMat();
            })
            .Aggregate((ele, next) =>
            {
                // 特徴点を検出
                // キーポイントを検出
                using var descriptors1 = new Mat();
                using var descriptors2 = new Mat();
                akaze.DetectAndCompute(ele, null, out var keyPoints1, descriptors1);
                akaze.DetectAndCompute(next, null, out var keyPoints2, descriptors2);

                // それぞれの特徴量をマッチング
                var matches = matcher.Match(descriptors1, descriptors2);
                if (matches.Length == 0) return ele;
                // キーポイントの情報で最も距離の近い（似ている）キーポイントを見る。
                var goodMatch = matches.MinBy(x => x.Distance);
                //using var drawMatch = new Mat();
                //Cv2.DrawMatches(ele, keyPoints1, next, keyPoints2, new[] { goodMatch }, drawMatch);
                //Cv2.ImShow("image match", drawMatch);
                //Cv2.WaitKey();

                var x1 = keyPoints1.ElementAt(goodMatch.QueryIdx).Pt.X;
                var x2 = keyPoints2.ElementAt(goodMatch.TrainIdx).Pt.X;
                var trimmedEle = ele.Clone(new Rect(0, 0, (int)x1, ele.Height));
                var trimmedNext = next.Clone(new Rect((int)x2, 0, next.Width - (int)x2, next.Height));
                var dst = new Mat();
                Cv2.HConcat(new[] { trimmedEle, trimmedNext }, dst);
                //Cv2.ImShow("concat", dst);
                //Cv2.WaitKey();

                return dst;
            })
            .Subscribe(mat =>
            {
                Cv2.ImShow("concat?", mat);
                Cv2.WaitKey();
            });
    }

    // テンプレートマッチングでマッチした箇所を四角で表示する
    public void MatchTemplate(string videoPath)
    {
        //using var left = new Mat(@"Resources/left.jpg");
        //using var center = new Mat(@"Resources/center.jpg");

        using var capture = VideoCapture.FromFile(videoPath);
        using var center = capture.RetrieveMat().Resize(Size.Zero, 0.1, 0.1, InterpolationFlags.Area);
        capture.PosMsec = 1000;
        using var left = capture.RetrieveMat().Resize(Size.Zero, 0.1, 0.1, InterpolationFlags.Area);

        var w = (int)(center.Width / 10d);
        var trimmingRect = new Rect(0, 0, w, center.Height);
        using var template = center.SubMat(trimmingRect);

        var leftMatch = Match(left, template);
        var centerMatch = Match(center, template);
        Cv2.ImShow("left", leftMatch);
        Cv2.ImShow("center", centerMatch);
        Cv2.WaitKey();

        Mat Match(Mat target, Mat temp)
        {
            target
                .MatchTemplate(temp, TemplateMatchModes.CCoeffNormed)
                .MinMaxLoc(out var minVal, out var maxVal, out var minLoc, out var maxLoc);
            if (maxVal >= 0.7)
            {
                //矩形と値を描画
                target.Rectangle(new Rect(maxLoc, temp.Size()), Scalar.Blue, 2);
                target.PutText(maxVal.ToString(), maxLoc, HersheyFonts.HersheyDuplex, 1, Scalar.Blue);
                return target;
            }

            return target;
        }
    }

    public void IterateMatchTemplate(string videoPath)
    {
        var cnt = 0;
        var capture = VideoCapture.FromFile(videoPath);

        // カメラが右から左へ進む想定
        var w = (int)(capture.FrameWidth / 100d);
        var startX = capture.FrameWidth / 2 - w / 2;
        var templateRect = new Rect(startX, 0, w, capture.FrameHeight);
        //Observable.Range(0, capture.FrameCount - 1)
        Observable.Range(0, 100)
            .Select(i =>
            {
                capture.PosMsec = i * 100;
                return capture.RetrieveMat().Resize(Size.Zero, 1, 1, InterpolationFlags.Area);
            })
            .Aggregate((ele, next) =>
            {
                cnt++;
                using var temp = ele.SubMat(templateRect);

                next
                    .MatchTemplate(temp, TemplateMatchModes.CCoeffNormed)
                    .MinMaxLoc(out var minVal, out var maxVal, out var minLoc, out var maxLoc);
                if (maxVal >= 0.7)
                {
                    var trimTarget = next.Clone(new Rect(maxLoc, temp.Size()));
                    var dst = new Mat();
                    Cv2.HConcat(new[] { trimTarget, next }, dst);
                    Debug.WriteLine($"Match 'N Concat1 Frame{cnt}");
                    return dst;
                }

                Debug.WriteLine($"Don't Match Frame{cnt}");
                return ele;
            })
            .Subscribe(mat =>
            {
                Cv2.ImShow("result", mat);
                Cv2.WaitKey();
                mat.SaveImage(
                    Path.Combine(
                        Environment.GetFolderPath(
                            Environment.SpecialFolder.Desktop),
                        "Result.png"));
            });
    }
}