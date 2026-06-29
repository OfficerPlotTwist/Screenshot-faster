using System.Windows;

namespace ScreenshotFaster;

public enum CaptureMode { Image, Video }

public partial class App : Application
{
    private Config _cfg = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _cfg = Config.Load();

        // Headless capture for scripting/testing: --shot x,y,w,h  (physical pixels)
        if (TryHeadlessShot(e.Args))
            return;

        var startMode = _cfg.DefaultMode?.Trim().ToLowerInvariant() == "video"
            ? CaptureMode.Video
            : CaptureMode.Image;

        // Show the full-screen region selector. It calls back with the result.
        var overlay = new OverlayWindow(startMode);
        overlay.Completed += OnRegionSelected;
        overlay.Cancelled += (_, _) => Shutdown();
        overlay.Show();
        overlay.Activate();
    }

    /// <summary>
    /// If invoked as `--shot x,y,w,h`, capture that physical-pixel region to a PNG,
    /// print the path to stdout, and exit without showing any UI. Returns true if handled.
    /// </summary>
    private bool TryHeadlessShot(string[] args)
    {
        int idx = Array.FindIndex(args, a => a.Equals("--shot", StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || idx + 1 >= args.Length)
            return false;

        var parts = args[idx + 1].Split(',', StringSplitOptions.TrimEntries);
        if (parts.Length != 4
            || !int.TryParse(parts[0], out int x) || !int.TryParse(parts[1], out int y)
            || !int.TryParse(parts[2], out int w) || !int.TryParse(parts[3], out int h)
            || w < 1 || h < 1)
        {
            Console.Error.WriteLine("Usage: ScreenshotFaster.exe --shot x,y,w,h");
            Shutdown(2);
            return true;
        }

        try
        {
            var outPath = _cfg.NewOutputPath("shot", "png");
            ScreenCapture.SavePng(new RegionPx(x, y, w, h), outPath);
            Console.Out.WriteLine(outPath);
            Shutdown(0);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Shutdown(1);
        }
        return true;
    }

    private void OnRegionSelected(object? sender, CaptureCompletedEventArgs e)
    {
        if (e.Mode == CaptureMode.Image)
        {
            DoImageCapture(e.Region);
        }
        else
        {
            DoVideoCapture(e.Region);
        }
    }

    private void DoImageCapture(RegionPx region)
    {
        try
        {
            var outPath = _cfg.NewOutputPath("shot", "png");
            ScreenCapture.SavePng(region, outPath);
            ShowToastThenExit($"Image saved to\n{outPath}");
        }
        catch (Exception ex)
        {
            ShowToastThenExit($"Capture failed:\n{ex.Message}");
        }
    }

    private void DoVideoCapture(RegionPx region)
    {
        Recorder recorder;
        try
        {
            recorder = new Recorder(_cfg, region);
            recorder.Start();
        }
        catch (Exception ex)
        {
            ShowToastThenExit($"Could not start recording:\n{ex.Message}\n\nIs ffmpeg installed / on PATH?");
            return;
        }

        var widget = new RecordingWidget();
        widget.StopRequested += (_, _) =>
        {
            string outPath;
            try
            {
                outPath = recorder.Stop();
            }
            catch (Exception ex)
            {
                widget.Close();
                ShowToastThenExit($"Recording error:\n{ex.Message}");
                return;
            }
            widget.Close();
            ShowToastThenExit($"Video saved to\n{outPath}");
        };
        widget.Show();
    }

    private void ShowToastThenExit(string message)
    {
        var toast = new SavedToast(message);
        toast.Closed += (_, _) => Shutdown();
        toast.Show();
    }
}
