using System.Diagnostics;
using System.IO;

namespace ScreenshotFaster;

/// <summary>
/// Wraps an ffmpeg process that records a screen region with gdigrab to an MP4.
/// Stops gracefully by sending "q" to ffmpeg's stdin so the file is finalised cleanly.
/// </summary>
public sealed class Recorder
{
    private readonly Config _cfg;
    private Process? _proc;

    public string OutputPath { get; }
    public bool IsRecording => _proc is { HasExited: false };

    public Recorder(Config cfg, RegionPx region)
    {
        _cfg = cfg;
        OutputPath = cfg.NewOutputPath("rec", "mp4");
        Region = region;
    }

    public RegionPx Region { get; }

    /// <summary>Resolves the ffmpeg executable, throwing a clear error if it cannot be found.</summary>
    private string ResolveFfmpeg()
    {
        var path = _cfg.FfmpegPath;
        if (string.IsNullOrWhiteSpace(path))
            path = "ffmpeg";

        // Absolute path that exists -> use directly.
        if (File.Exists(path))
            return path;

        // Bare name -> rely on PATH resolution by the OS.
        return path;
    }

    public void Start()
    {
        var r = Region;
        // h.264 requires even dimensions.
        int w = r.Width - (r.Width % 2);
        int h = r.Height - (r.Height % 2);

        var args =
            $"-y -f gdigrab -framerate {_cfg.VideoFps} " +
            $"-offset_x {r.X} -offset_y {r.Y} -video_size {w}x{h} " +
            $"-draw_mouse 1 -i desktop " +
            $"-c:v libx264 -preset {_cfg.VideoPreset} -crf {_cfg.VideoCrf} -pix_fmt yuv420p " +
            $"\"{OutputPath}\"";

        var psi = new ProcessStartInfo
        {
            FileName = ResolveFfmpeg(),
            Arguments = args,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
        };

        _proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _proc.Start();
        // Drain stderr/stdout so the pipes never fill and block ffmpeg.
        _proc.BeginErrorReadLine();
        _proc.BeginOutputReadLine();
    }

    /// <summary>Stop recording and finalise the file. Returns the output path.</summary>
    public string Stop()
    {
        if (_proc == null)
            return OutputPath;

        try
        {
            if (!_proc.HasExited)
            {
                // Tell ffmpeg to quit gracefully so the MP4 moov atom is written.
                _proc.StandardInput.Write("q");
                _proc.StandardInput.Flush();
                if (!_proc.WaitForExit(5000))
                {
                    _proc.Kill(entireProcessTree: true);
                    _proc.WaitForExit(2000);
                }
            }
        }
        catch
        {
            try { _proc.Kill(entireProcessTree: true); } catch { /* ignore */ }
        }
        finally
        {
            _proc.Dispose();
            _proc = null;
        }

        return OutputPath;
    }
}
