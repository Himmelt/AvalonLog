using System.Windows.Media;

namespace AvalonLog.Core;

public static class BrushHelper {
    public static SolidColorBrush FreezeIt(SolidColorBrush br) {
        if (!br.IsFrozen && br.CanFreeze) br.Freeze();
        return br;
    }

    public static byte ClampToByte(int i) {
        if (i <= 0) return 0;
        if (i >= 255) return 255;
        return (byte)i;
    }

    public static SolidColorBrush OfRGB(int r, int g, int b) {
        return FreezeIt(new SolidColorBrush(Color.FromArgb(255, ClampToByte(r), ClampToByte(g), ClampToByte(b))));
    }

    public static SolidColorBrush OfARGB(int a, int r, int g, int b) {
        return FreezeIt(new SolidColorBrush(Color.FromArgb(ClampToByte(a), ClampToByte(r), ClampToByte(g), ClampToByte(b))));
    }

    public static Color ChangeLuminance(int amount, Color col) {
        byte r = ClampToByte(col.R + amount);
        byte g = ClampToByte(col.G + amount);
        byte b = ClampToByte(col.B + amount);
        return Color.FromArgb(col.A, r, g, b);
    }

    public static SolidColorBrush Brighter(int amount, SolidColorBrush br) {
        return FreezeIt(new SolidColorBrush { Color = ChangeLuminance(amount, br.Color) });
    }

    public static SolidColorBrush Darker(int amount, SolidColorBrush br) {
        return FreezeIt(new SolidColorBrush { Color = ChangeLuminance(-amount, br.Color) });
    }
}

public static class PenHelper {
    public static Pen FreezeIt(Pen pen) {
        if (!pen.IsFrozen && pen.CanFreeze) pen.Freeze();
        return pen;
    }
}
