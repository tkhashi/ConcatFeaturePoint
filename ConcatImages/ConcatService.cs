using System.Collections.Concurrent;
using System.Diagnostics;
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

    public Mat IterateMatchTemplate(string videoPath, int skipFrameCount)
    {
        var capture = VideoCapture.FromFile(videoPath);

        // カメラが右から左へ進む想定
        var ratio = 0.1;
        var w = (capture.FrameWidth * ratio / 100d);
        var startX = (capture.FrameWidth *ratio / 2 - w / 2);
        var templateRect = new Rect((int)startX, 0, (int)w, (int)(capture.FrameHeight * ratio));

        var result = new Mat();
        Observable.Range(0, 100)
            .Select(i =>
            {
                capture.PosMsec = i * 100 + skipFrameCount;
                return capture.RetrieveMat().Resize(Size.Zero, ratio, ratio, InterpolationFlags.Area);
            })
            .Aggregate((ele, next) =>
            {
                using var template = ele.SubMat(templateRect);

                next
                    .MatchTemplate(template, TemplateMatchModes.CCoeffNormed)
                    .MinMaxLoc(out _, out var maxVal, out _, out var maxLoc);
                if (maxVal >= 0.7)
                {
                    var trimTarget = next.Clone(new Rect(maxLoc, template.Size()));
                    var dst = new Mat();
                    Cv2.HConcat(new[] { trimTarget, next }, dst);
                    return dst;
                }

                return ele;
            })
            .Subscribe(mat =>
            {
                //Cv2.ImShow("result", mat);
                //Cv2.WaitKey();
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                mat.SaveImage(Path.Combine(desktop, $"Result{skipFrameCount}frame.png"));

                result = mat;
            });

        Console.WriteLine("DONE!!");
        return result;
    }

    public static void TrimmingTimer(string videoPath)
    {
        var sw = Stopwatch.StartNew();
        using var cap = VideoCapture.FromFile(videoPath);
        using var mockFrame = cap.RetrieveMat();
        var startRow = (mockFrame.Height / 2) - 25;
        var endRow = startRow + 25;
        var startCol = (mockFrame.Width / 2) - 25;
        var endCol = startCol + 25;
        var framePairs = new ConcurrentDictionary<int, Mat>();

        for (var i = 1; i < cap.FrameCount; i+=300)
        {
            if (i > cap.FrameCount) break;
            GetFrames(i);
            SubMats();
        }

        void SubMats()
        {
            if (framePairs is null) return;
            framePairs.AsParallel().ForAll(x =>
            {
                var trimmed = x.Value.SubMat(startRow, endRow, startCol, endCol);
                framePairs.AddOrUpdate(x.Key, i => trimmed, (i, mat) => trimmed);
            });
        }

        void GetFrames(int startFrame)
        {
            if (framePairs is null) return;
            for (var i = startFrame; i < startFrame + 300; i++)
            {
                if (i > cap.FrameCount) break;
                cap.PosFrames = i;
                var frame = cap.RetrieveMat();
                framePairs.AddOrUpdate(i, _ => frame, (_, _) => frame);
            }
        }

        sw.Stop();
        var time = sw.Elapsed.TotalSeconds;
        Debug.WriteLine(time);
        Console.WriteLine(time);
        Console.ReadKey();
    }
    public IEnumerable<FrameAxis> MatchAxisFrame(string videoPath)
    {

        //    // OpenCvSharpを利用

        //    // 現在のフレームと次のフレームを取得

        //    // それぞれ1/10のサイズに縮小

        //    // 現在のフレームと次のフレームの中央100*100をテンプレートマッチングで比較して移動量をFramAxis構造体として返す

        //    // 

        return new List<FrameAxis>();
    }


}

public struct FrameAxis
{
    public double X { get; set; }
    public double Y { get; set; }
    public int Frame { get; set; }
}