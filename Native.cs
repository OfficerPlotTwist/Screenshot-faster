using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ScreenshotFaster;

/// <summary>Win32 interop helpers.</summary>
internal static class Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetCursorPos(out POINT lpPoint);

    /// <summary>Current cursor position in physical screen pixels.</summary>
    public static POINT CursorPos
    {
        get
        {
            GetCursorPos(out var p);
            return p;
        }
    }

    /// <summary>
    /// DPI scale of the monitor a window currently sits on. With System DPI awareness
    /// this is uniform across the desktop, so it is safe to use for capture math.
    /// </summary>
    public static (double X, double Y) GetDpiScale(Window w)
    {
        var src = PresentationSource.FromVisual(w);
        if (src?.CompositionTarget != null)
        {
            var m = src.CompositionTarget.TransformToDevice;
            return (m.M11, m.M22);
        }
        return (1.0, 1.0);
    }
}
