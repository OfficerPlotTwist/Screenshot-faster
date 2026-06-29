# Screenshot Faster

A fast, single-shot region **screenshot** and **screen-recording** tool for Windows.
Launch it, drag a region, done — the app does one thing and exits, so it stays out of the way.

## How it works

1. **Launch** the app (e.g. from its pinned taskbar icon).
2. A dimmed full-screen overlay appears. A badge at the top shows the current mode.
   - Press **Tab** to switch between **IMAGE** and **VIDEO** capture.
   - Press **Esc** to cancel.
3. **Drag** a rectangle over the area you want.
4. **Image mode:** the region is saved as a PNG and a toast shows
   `Image saved to <path>`, then the app closes.
5. **Video mode:** recording starts immediately and a draggable **red record button**
   appears on top of everything.
   - **Click** it to stop. The video is saved and a `Video saved to <path>` toast appears.
   - **Press-and-drag** it to move it out of the way — it won't stop until you click.

## Configuration

On first run a config file is created at:

```
%APPDATA%\ScreenshotFaster\config.json
```

| Key               | Default                              | Meaning                                            |
|-------------------|--------------------------------------|----------------------------------------------------|
| `SaveLocation`    | `%USERPROFILE%\Pictures\ScreenshotFaster` | Where screenshots and recordings are saved.   |
| `FfmpegPath`      | `ffmpeg`                             | Path to ffmpeg (`ffmpeg` = resolve from PATH).     |
| `VideoFps`        | `30`                                 | Recording frame rate.                              |
| `VideoPreset`     | `veryfast`                           | x264 speed/size tradeoff (`ultrafast`..`veryslow`).|
| `VideoCrf`        | `23`                                 | x264 quality (lower = better/larger, 18–28 typical).|
| `TimestampPattern`| `yyyyMMdd_HHmmss`                    | Timestamp used in filenames.                       |
| `DefaultMode`     | `image`                              | Mode the overlay starts in (`image` or `video`).   |

Set **`SaveLocation`** to your preferred folder. The folder is created if missing.

## Requirements

- **Windows 10/11 (x64).** The published exe is self-contained — no .NET install needed.
- **ffmpeg** is required for **video recording only** (screenshots work without it).
  Install with `winget install Gyan.FFmpeg` and make sure it's on PATH, or set
  `FfmpegPath` in the config to the full `ffmpeg.exe` path.

## Build & install from source

Requires the .NET 8 SDK.

```powershell
# Build
dotnet build -c Release

# Publish a single self-contained exe + install + create a pinnable shortcut
powershell -ExecutionPolicy Bypass -File install.ps1
```

`install.ps1` copies the exe to `%LOCALAPPDATA%\ScreenshotFaster`, creates a Start Menu
shortcut, and opens its folder. To pin: **Start → type "Screenshot Faster" → right-click →
Pin to taskbar** (or launch it and right-click the taskbar icon → Pin to taskbar).

## Notes

- Capture coordinates are computed in physical pixels with System-DPI awareness, so the
  saved image/video matches exactly what you selected on a uniform-DPI desktop.
- The overlay, record button, and toast are all hidden from the taskbar, so launching the
  tool never clutters your taskbar with extra buttons.
- Cross-platform note: the capture/recording layer is isolated (`ScreenCapture.cs`,
  `Recorder.cs`). A Linux port would swap GDI for an X11/PipeWire grab and ffmpeg's
  `gdigrab` for `x11grab`; the WPF UI would move to Avalonia.
