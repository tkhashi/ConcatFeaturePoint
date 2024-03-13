using System.Collections.Concurrent;
using System.Diagnostics;
using OpenCvSharp;

var destop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
var videoPath = Path.Combine(destop, @"RoofInspection\DJI_0327.MP4");
using var cap = VideoCapture.FromFile(videoPath);
var startRow = cap.FrameHeight / 2 - 25;
var endRow = startRow + 25;
var startCol = cap.FrameWidth / 2 - 25;
var endCol = startCol + 25;
var framePairs = new ConcurrentDictionary<int, Mat>();

var sw = Stopwatch.StartNew();
//start from second
cap.PosMsec = 100000;
for (var i = 0; i < 10; i ++)
{
    // TODO: 最後の時間入れる
    cap.PosMsec += i * 10;
    var frame = new Mat();
    cap.Read(frame);
    framePairs.AddOrUpdate(i, _ => frame, (_, _) => frame);
}

await ResizeParallel();

var lastResult = Match();
sw.Stop();
var t = sw.Elapsed.TotalSeconds;
Console.WriteLine($"掛かった時間は... {t}");
Console.ReadKey();
Cv2.ImShow("result", lastResult);
Cv2.WaitKey();

foreach (var (key, value) in framePairs)
{
    Cv2.ImShow($"{key}番目", value);
    Cv2.WaitKey();
}


void GetFrames()
{
    for (var i = 0; i < 10; i++)
    {
        var frame = cap.RetrieveMat();
        framePairs.AddOrUpdate(i, _ => frame, (_, _) => frame);
    }
}

// bugの中をすべてトリミング
void SubMatParallel()
{
    framePairs.AsParallel().ForAll(x =>
    {
        var trimmed = x.Value.SubMat(startRow, endRow, startCol, endCol);
        framePairs.AddOrUpdate(x.Key, i => trimmed, (i, mat) => trimmed);
        x.Value.Dispose();
    });
}

async Task ResizeParallel()
{
    await Task.Factory.StartNew(() =>
    {
        framePairs.AsParallel().ForAll(x =>
        {
            var resized = x.Value.Resize(Size.Zero, 0.1, 0.1, InterpolationFlags.Area);
            framePairs.AddOrUpdate(x.Key, _ => resized, (_, _) => resized);
            x.Value.Dispose();
        });
    });

}

Mat Match()
{
    var res = framePairs.Aggregate((ele, next) =>
    {
        next.Value
            .MatchTemplate(ele.Value, TemplateMatchModes.CCoeffNormed)
            .MinMaxLoc(out _, out var maxVal, out _, out var maxLoc);
        if (maxVal >= 0.7)
        {
            var trimTarget = next.Value.Clone(new Rect(maxLoc, ele.Value.Size()));
            //var dst = new Mat();
            //Cv2.HConcat(new[] { trimTarget, next }, dst);
            //return dst;


            Console.WriteLine("MATCH!");
            var mats = new Mat[]
            {
                trimTarget,
                next.Value
            };
            var result = new Mat();
            Cv2.VConcat(mats, result);

            return new KeyValuePair<int, Mat>(0, result);
        }
        else
        {
            Console.WriteLine("DIS MATCH!");
        }
        return next;
    });

    return res.Value;

}

