using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace ScreenshotFaster;

public sealed class CaptureCompletedEventArgs : EventArgs
{
    public required CaptureMode Mode { get; init; }
    public required RegionPx Region { get; init; }
}

public partial class OverlayWindow : Window
{
    public event EventHandler<CaptureCompletedEventArgs>? Completed;
    public event EventHandler? Cancelled;

    private CaptureMode _mode;
    private Point _start;
    private bool _dragging;
    private (double X, double Y) _dpi = (1.0, 1.0);

    private static readonly Brush ImageBrush = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84)); // green
    private static readonly Brush VideoBrush = new SolidColorBrush(Color.FromRgb(0xFF, 0x4D, 0x4D)); // red

    public OverlayWindow(CaptureMode startMode)
    {
        InitializeComponent();
        _mode = startMode;

        // Cover the entire virtual desktop (all monitors), in DIP.
        Left = SystemParameters.VirtualScreenLeft;
        Top = SystemParameters.VirtualScreenTop;
        Width = SystemParameters.VirtualScreenWidth;
        Height = SystemParameters.VirtualScreenHeight;

        Loaded += OnLoaded;
        SourceInitialized += (_, _) => _dpi = Native.GetDpiScale(this);

        KeyDown += OnKeyDown;
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        Root.Width = Width;
        Root.Height = Height;
        UpdateDim(null);
        UpdateHint();
        PositionHintBadge();
        Activate();
        Focus();
    }

    private void UpdateHint()
    {
        bool image = _mode == CaptureMode.Image;
        ModeDot.Fill = image ? ImageBrush : VideoBrush;
        SelRect.Stroke = image ? ImageBrush : VideoBrush;
        HintText.Text = image
            ? "IMAGE  —  drag to capture     ·     Tab: switch to VIDEO     ·     Esc: cancel"
            : "VIDEO  —  drag to record      ·     Tab: switch to IMAGE     ·     Esc: cancel";
    }

    private void PositionHintBadge()
    {
        HintBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double bw = HintBadge.DesiredSize.Width;
        Canvas.SetLeft(HintBadge, (Width - bw) / 2);
        Canvas.SetTop(HintBadge, 48);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            Cancelled?.Invoke(this, EventArgs.Empty);
        }
        else if (e.Key == Key.Tab)
        {
            e.Handled = true;
            _mode = _mode == CaptureMode.Image ? CaptureMode.Video : CaptureMode.Image;
            UpdateHint();
            PositionHintBadge();
        }
    }

    private void OnMouseDown(object? sender, MouseButtonEventArgs e)
    {
        _start = e.GetPosition(Root);
        _dragging = true;
        SelRect.Visibility = Visibility.Visible;
        SizeBadge.Visibility = Visibility.Visible;
        CaptureMouse();
    }

    private void OnMouseMove(object? sender, MouseEventArgs e)
    {
        if (!_dragging) return;
        var p = e.GetPosition(Root);
        var rect = MakeRect(_start, p);

        Canvas.SetLeft(SelRect, rect.X);
        Canvas.SetTop(SelRect, rect.Y);
        SelRect.Width = rect.Width;
        SelRect.Height = rect.Height;

        UpdateDim(rect);

        // Size readout (physical pixels), placed just above/below the selection.
        int pw = (int)Math.Round(rect.Width * _dpi.X);
        int ph = (int)Math.Round(rect.Height * _dpi.Y);
        SizeText.Text = $"{pw} x {ph}";
        SizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        double sx = rect.X;
        double sy = rect.Y - SizeBadge.DesiredSize.Height - 4;
        if (sy < 0) sy = rect.Y + rect.Height + 4;
        Canvas.SetLeft(SizeBadge, sx);
        Canvas.SetTop(SizeBadge, sy);
    }

    private void OnMouseUp(object? sender, MouseButtonEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;
        ReleaseMouseCapture();

        var end = e.GetPosition(Root);
        var rect = MakeRect(_start, end);

        // Convert DIP rect -> physical pixel rect for capture/recording.
        double absX = Left + rect.X;
        double absY = Top + rect.Y;
        int px = (int)Math.Round(absX * _dpi.X);
        int py = (int)Math.Round(absY * _dpi.Y);
        int pw = (int)Math.Round(rect.Width * _dpi.X);
        int ph = (int)Math.Round(rect.Height * _dpi.Y);

        // Ignore accidental clicks / tiny drags.
        if (pw < 8 || ph < 8)
        {
            SelRect.Visibility = Visibility.Collapsed;
            SizeBadge.Visibility = Visibility.Collapsed;
            UpdateDim(null);
            return;
        }

        var region = new RegionPx(px, py, pw, ph);
        var mode = _mode;
        Close();
        Completed?.Invoke(this, new CaptureCompletedEventArgs { Mode = mode, Region = region });
    }

    private static Rect MakeRect(Point a, Point b)
    {
        double x = Math.Min(a.X, b.X);
        double y = Math.Min(a.Y, b.Y);
        double w = Math.Abs(a.X - b.X);
        double h = Math.Abs(a.Y - b.Y);
        return new Rect(x, y, w, h);
    }

    /// <summary>Redraw the dim layer, punching a clear hole where the selection is.</summary>
    private void UpdateDim(Rect? hole)
    {
        var full = new RectangleGeometry(new Rect(0, 0, Width, Height));
        if (hole is { Width: > 0, Height: > 0 } h)
        {
            var group = new GeometryGroup { FillRule = FillRule.EvenOdd };
            group.Children.Add(full);
            group.Children.Add(new RectangleGeometry(h));
            DimPath.Data = group;
        }
        else
        {
            DimPath.Data = full;
        }
    }
}
