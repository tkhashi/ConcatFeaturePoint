using OpenCvSharp;

public class VideoProcessor
{
    public static Mat GenerateCompositeImageFromVideo(string videoFilePath)
    {
        if (string.IsNullOrEmpty(videoFilePath))
            throw new ArgumentException("Video file path cannot be null or empty.");

        using var capture = new VideoCapture(videoFilePath);
        if (!capture.IsOpened()) throw new InvalidOperationException("Unable to open the video file.");

        var frames = new List<Mat>();

        for (var i = 1; i < capture.FrameCount; i++)
        {
            var currentFrame = new Mat();
            capture.Read(currentFrame);
            if (currentFrame.Empty())
            {
                currentFrame.Dispose();
                break;
            }

            var resizedFrame = new Mat();
            Cv2.Resize(currentFrame, resizedFrame, new Size(currentFrame.Width / 2, currentFrame.Height / 2));
            frames.Add(resizedFrame);
            currentFrame.Dispose();
        }

        var compositeImage = MergeFrames(frames);
        return compositeImage;
    }

    private static Mat MergeFrames(List<Mat> frames)
    {
        if (frames == null || frames.Count == 0) throw new ArgumentException("Frames cannot be null or empty.");

        var compositeImage = new Mat();
        frames[0].CopyTo(compositeImage);
        frames[0].Dispose();

        for (var i = 1; i < frames.Count; i++)
        {
            var currentFrame = frames[i];
            var offset = CalculateOffset(compositeImage, currentFrame);
            var mergedImage =
                new Mat(
                    new Size(compositeImage.Width + currentFrame.Width - offset.X,
                        Math.Max(compositeImage.Height, currentFrame.Height + offset.Y)), compositeImage.Type());

            compositeImage.CopyTo(new Mat(mergedImage, new Rect(0, 0, compositeImage.Width, compositeImage.Height)));
            currentFrame.CopyTo(new Mat(mergedImage,
                new Rect(offset.X, offset.Y, currentFrame.Width, currentFrame.Height)));

            mergedImage.CopyTo(compositeImage);
            mergedImage.Dispose();
            frames[i].Dispose();
        }

        return compositeImage;
    }

    private static Point CalculateOffset(Mat image1, Mat image2)
    {
        var result = new Mat();
        Cv2.MatchTemplate(image1, image2, result, TemplateMatchModes.SqDiff);
        double minVal, maxVal;
        Point minLoc, maxLoc;
        Cv2.MinMaxLoc(result, out minVal, out maxVal, out minLoc, out maxLoc);
        result.Dispose();

        return minLoc;
    }
}