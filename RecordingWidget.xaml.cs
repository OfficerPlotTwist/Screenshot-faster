using System.Windows;
using System.Windows.Input;

namespace ScreenshotFaster;

/// <summary>
/// Always-on-top record indicator. A single left-click stops recording; press-and-drag
/// moves it anywhere without stopping. Click vs. drag is decided by a movement threshold.
/// </summary>
public partial class RecordingWidget : Window
{
    /// <summary>Raised when the user clicks the widget to stop recording.</summary>
    public event EventHandler? StopRequested;

    private const double DragThresholdPx = 5.0;

    private bool _pressed;
    private bool _moved;
    private Native.POINT _downCursor;   // physical px at press
    private double _winLeftAtDown;       // DIP
    private double _winTopAtDown;        // DIP
    private (double X, double Y) _dpi = (1.0, 1.0);

    public RecordingWidget()
    {
        InitializeComponent();

        SourceInitialized += (_, _) =>
        {
            _dpi = Native.GetDpiScale(this);
            PlaceNearCursor();
        };

        MouseLeftButtonDown += OnDown;
        MouseMove += OnMove;
        MouseLeftButtonUp += OnUp;
    }

    private void PlaceNearCursor()
    {
        // Drop the widget just below-right of the cursor, clamped to the work area.
        var c = Native.CursorPos;
        double leftDip = c.X / _dpi.X + 16;
        double topDip = c.Y / _dpi.Y + 16;

        var wa = SystemParameters.WorkArea;
        leftDip = Math.Min(leftDip, wa.Right - ActualWidth - 8);
        topDip = Math.Min(topDip, wa.Bottom - ActualHeight - 8);
        leftDip = Math.Max(leftDip, wa.Left + 8);
        topDip = Math.Max(topDip, wa.Top + 8);

        Left = leftDip;
        Top = topDip;
    }

    private void OnDown(object? sender, MouseButtonEventArgs e)
    {
        _pressed = true;
        _moved = false;
        _downCursor = Native.CursorPos;
        _winLeftAtDown = Left;
        _winTopAtDown = Top;
        CaptureMouse();
    }

    private void OnMove(object? sender, MouseEventArgs e)
    {
        if (!_pressed) return;

        var cur = Native.CursorPos;
        double dxPx = cur.X - _downCursor.X;
        double dyPx = cur.Y - _downCursor.Y;

        if (!_moved && Math.Abs(dxPx) < DragThresholdPx && Math.Abs(dyPx) < DragThresholdPx)
            return; // still within the click tolerance

        _moved = true;
        Left = _winLeftAtDown + dxPx / _dpi.X;
        Top = _winTopAtDown + dyPx / _dpi.Y;
    }

    private void OnUp(object? sender, MouseButtonEventArgs e)
    {
        if (!_pressed) return;
        _pressed = false;
        ReleaseMouseCapture();

        if (!_moved)
            StopRequested?.Invoke(this, EventArgs.Empty);
    }
}
