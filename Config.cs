using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScreenshotFaster;

/// <summary>
/// User configuration, persisted as JSON in %APPDATA%\ScreenshotFaster\config.json.
/// Created with sensible defaults on first run; edit the file to change behaviour.
/// </summary>
public sealed class Config
{
    /// <summary>Folder where screenshots and recordings are written.</summary>
    public string SaveLocation { get; set; } = DefaultSaveLocation;

    /// <summary>Path to ffmpeg. "ffmpeg" resolves it from PATH.</summary>
    public string FfmpegPath { get; set; } = "ffmpeg";

    /// <summary>Recording frame rate.</summary>
    public int VideoFps { get; set; } = 30;

    /// <summary>x264 preset (ultrafast..veryslow). Faster = larger files, less CPU.</summary>
    public string VideoPreset { get; set; } = "veryfast";

    /// <summary>Constant Rate Factor for x264 (lower = better quality/larger). 18-28 typical.</summary>
    public int VideoCrf { get; set; } = 23;

    /// <summary>Filename timestamp pattern (.NET DateTime format).</summary>
    public string TimestampPattern { get; set; } = "yyyyMMdd_HHmmss";

    /// <summary>Which mode the overlay starts in: "image" or "video".</summary>
    public string DefaultMode { get; set; } = "image";

    // ---- infrastructure ----

    [JsonIgnore]
    public static string ConfigDir =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ScreenshotFaster");

    [JsonIgnore]
    public static string ConfigPath => Path.Combine(ConfigDir, "config.json");

    [JsonIgnore]
    public static string DefaultSaveLocation =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "ScreenshotFaster");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    };

    /// <summary>Load config, creating defaults on disk if absent. Never throws.</summary>
    public static Config Load()
    {
        Config cfg;
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                cfg = JsonSerializer.Deserialize<Config>(json, JsonOpts) ?? new Config();
            }
            else
            {
                cfg = new Config();
                cfg.Save(); // write a starter file the user can edit
            }
        }
        catch
        {
            cfg = new Config();
        }

        // Ensure the save folder exists so capture never fails on a missing dir.
        try { Directory.CreateDirectory(cfg.SaveLocation); } catch { /* best effort */ }
        return cfg;
    }

    public void Save()
    {
        Directory.CreateDirectory(ConfigDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(this, JsonOpts));
    }

    /// <summary>Build a full output path for a capture of the given extension (no dot).</summary>
    public string NewOutputPath(string prefix, string extension)
    {
        Directory.CreateDirectory(SaveLocation);
        var stamp = DateTime.Now.ToString(TimestampPattern);
        return Path.Combine(SaveLocation, $"{prefix}_{stamp}.{extension}");
    }
}
