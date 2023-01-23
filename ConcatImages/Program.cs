using ConcatImages;

const string videoPath = @"C:\Users\Kazuhiro Takahashi\Desktop\roofInspection\DJI_0299.MP4";
var concater = new ConcatService();
concater.Concat(videoPath);

//using OpenCvSharp;
//using System.Diagnostics;

//using var right = new Mat(@"Resources/right.jpg");
//using var center = new Mat(@"Resources/center.jpg");
//using var left = new Mat(@"Resources/left.jpg");
//using var full = new Mat(@"Resources/full.jpg");

////Hoge(right, center);
////Stitch(center, full);
//var trims = TrimStrip(full, 50, 10);
//using var r = Stitch(trims.ToArray());
//Cv2.ImShow("result", r);
//Cv2.WaitKey();

//Mat Stitch(params Mat[] param)
//{
//    var seed = param[0];
//    param.ToList().RemoveAt(0);
//    var result = param.Aggregate(seed, (ele, next) => StitchPair(ele, next));

//    //Cv2.ImShow("pair", result);
//    //Cv2.WaitKey();
//    return result;
//}

//Mat StitchPair(Mat one, Mat two)
//{
//    Cv2.ImShow("one", one);
//    Cv2.WaitKey();
//    Cv2.ImShow("two", two);
//    Cv2.WaitKey();
//    var inputs = new List<Mat>() { one, two};
//    var resultPanorama = new Mat();
//    var stitcher = Stitcher.Create(Stitcher.Mode.Scans);
//    var status = stitcher.Stitch(inputs, resultPanorama);

//    if (status is not Stitcher.Status.OK) return two;
//    //Cv2.ImShow("pair", resultPanorama);
//    //Cv2.WaitKey();
//    return resultPanorama;
//}

//IEnumerable<Mat> TrimStrip(Mat img, int equiDistant, int overrapWidth)
//{
//    var imgFullWidth = img.Width;
//    var trimWidth = imgFullWidth / equiDistant + (imgFullWidth / (equiDistant * overrapWidth));

//    var trims = Enumerable.Range(0, equiDistant)
//        .Select(i => 
//        {
//            var startX = i * (imgFullWidth / equiDistant);
//            var w = i * (imgFullWidth / equiDistant) + trimWidth > imgFullWidth
//                ? imgFullWidth - startX
//                : trimWidth;
//            var trimed = img.Clone(new Rect(startX, 0, w, img.Height));
//            return trimed;
//        })
//        .ToArray();

//    //foreach(var trimmed in trims)
//    //{
//    //    Cv2.ImShow("trimmed", trimmed);
//    //    Cv2.WaitKey();
//    //}

//    return trims;
//}

//void StitchVideoFrame(string videoPath)
//{
//    Mat result = null;
//    using var capture = VideoCapture.FromFile(videoPath);
//    for (int i = 0; i < capture.FrameCount - 1; i++)
//    {
//        capture.PosFrames = i;
//        using var mat = capture.RetrieveMat();
//        // TODO: unfold
//        // using var unfolded = Unfold(mat);
//        if (result is null)
//        {
//            // result = unfolded;
//            result = mat;
//            continue;
//        }

//        //result = Stitch(result, unfolded);
//        result = Stitch(result, mat);
//    }
//    Cv2.ImShow("stitched", result);
//    Cv2.WaitKey();
//}

//void Hoge(Mat s1, Mat s2)
//{
//    var src = new Mat[2];
//    var dst = new Mat[2];
//    using var outImg = new Mat();
//    using var descripter1 = new Mat();
//    using var descripter2 = new Mat();
//    using var result = new Mat();

//    src[0] = s1;
//    src[1] = s2;
//    dst[0] = new Mat();
//    dst[1] = new Mat();
//    Cv2.CvtColor(src[0], dst[0], ColorConversionCodes.BGR2GRAY);
//    Cv2.CvtColor(src[1], dst[1], ColorConversionCodes.BGR2GRAY);

//    var akaze = AKAZE.Create();
//    akaze.DetectAndCompute(dst[0], null, out KeyPoint[] keypoints1, descripter1);
//    akaze.DetectAndCompute(dst[1], null, out KeyPoint[] keypoints2, descripter2);

//    var matcher = new BFMatcher();
//    var matches = matcher.Match(descripter1, descripter2);

//    Cv2.DrawMatches(src[0], keypoints1, src[1], keypoints2, matches, outImg);

//    Cv2.ImShow("outimg", outImg);
//    Cv2.WaitKey();

//    //画像合体
//    var size = matches.Length;
//    var getPoints1 = new Vec2f[size];
//    var getPoints2 = new Vec2f[size];

//    for (var a = 0; a < size; a++)
//    {
//        var pt1 = keypoints1[matches[a].QueryIdx].Pt;
//        var pt2 = keypoints2[matches[a].TrainIdx].Pt;
//        getPoints1[a][0] = pt1.X;
//        getPoints1[a][1] = pt1.Y;
//        getPoints2[a][0] = pt2.X;
//        getPoints2[a][1] = pt2.Y;
//        Debug.WriteLine($"{pt2.X - pt1.X}, {pt2.Y - pt1.Y}");
//    }

//    var hom = Cv2.FindHomography(InputArray.Create(getPoints1), InputArray.Create(getPoints2), HomographyMethods.Ransac);

//    Cv2.WarpPerspective(src[0], result, hom, new Size(src[0].Cols * 2.0, src[0].Rows * 2.0));
//    for (int y = 0; y < src[0].Rows; y++)
//    {
//        for (int x = 0; x < src[0].Cols; x++)
//        {
//            result.Set(y, x, src[1].At<Vec3b>(y, x));
//        }
//    }

//    Cv2.ImShow("result", result);
//    Cv2.WaitKey();
//}

