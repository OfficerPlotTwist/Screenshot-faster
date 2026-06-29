using System.Windows;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ScreenshotFaster;

/// <summary>Brief auto-dismissing confirmation toast, anchored bottom-right.</summary>
public partial class SavedToast : Window
{
    private readonly DispatcherTimer _timer;

    public SavedToast(string message)
    {
        InitializeComponent();
        MessageText.Text = message;

        Loaded += OnLoaded;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1900) };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            FadeOutAndClose();
        };
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        var wa = SystemParameters.WorkArea;
        Left = wa.Right - ActualWidth - 24;
        Top = wa.Bottom - ActualHeight - 24;

        BeginAnimation(OpacityProperty,
            new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));

        _timer.Start();
    }

    private void FadeOutAndClose()
    {
        var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(220));
        fade.Completed += (_, _) => Close();
        BeginAnimation(OpacityProperty, fade);
    }
}
