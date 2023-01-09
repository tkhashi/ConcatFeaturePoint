using OpenCvSharp;
using System.Diagnostics;

using var right = new Mat(@"Resources/right.jpg");
using var center = new Mat(@"Resources/center.jpg");

Hoge(right, center);

void Hoge(Mat s1, Mat s2)
{
    var src = new Mat[2];
    var dst = new Mat[2];
    Mat outImg = new Mat();
    KeyPoint[] keypoints1;
    KeyPoint[] keypoints2;
    var descripter1 = new Mat();
    var descripter2 = new Mat();
    var result = new Mat();

    src[0] = s1;
    src[1] = s2;
    dst[0] = new Mat();
    dst[1] = new Mat();
    Cv2.CvtColor(src[0], dst[0], ColorConversionCodes.BGR2GRAY);
    Cv2.CvtColor(src[1], dst[1], ColorConversionCodes.BGR2GRAY);

    var akaze = AKAZE.Create();
    akaze.DetectAndCompute(dst[0], null, out keypoints1, descripter1);
    akaze.DetectAndCompute(dst[1], null, out keypoints2, descripter2);

    var matcher = new BFMatcher();
    var matches = matcher.Match(descripter1, descripter2);

    Cv2.DrawMatches(src[0], keypoints1, src[1], keypoints2, matches, outImg);

    using (new Window("OutImg", outImg))
    {
        Cv2.WaitKey();
    }

    //画像合体
    var size = matches.Length;
    var getPoints1 = new Vec2f[size];
    var getPoints2 = new Vec2f[size];

    for (var a = 0; a < size; a++)
    {
        var pt1 = keypoints1[matches[a].QueryIdx].Pt;
        var pt2 = keypoints2[matches[a].TrainIdx].Pt;
        getPoints1[a][0] = pt1.X;
        getPoints1[a][1] = pt1.Y;
        getPoints2[a][0] = pt2.X;
        getPoints2[a][1] = pt2.Y;
        Debug.WriteLine($"{pt2.X - pt1.X}, {pt2.Y - pt1.Y}");
    }

    var hom = Cv2.FindHomography(InputArray.Create(getPoints1), InputArray.Create(getPoints2), HomographyMethods.Ransac);

    Cv2.WarpPerspective(src[0], result, hom, new Size(src[0].Cols * 2.0, src[0].Rows * 2.0));
    for (int y = 0; y < src[0].Rows; y++)
    {
        for (int x = 0; x < src[0].Cols; x++)
        {
            result.Set(y, x, src[1].At<Vec3b>(y, x));
        }
    }

    using (new Window("result", result))
    {
        Cv2.WaitKey();
    }
}

