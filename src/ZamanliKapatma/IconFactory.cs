using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace ZamanliKapatma;

/// <summary>Durum rengine göre saat simgesini kodla çizer (ayrı .ico dosyasına gerek kalmaz).</summary>
internal static class IconFactory
{
    public static Icon CreateClockIcon(Color color, int size = 32)
    {
        using var bitmap = new Bitmap(size, size);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.Clear(Color.Transparent);

            float stroke = size * 0.11f;
            float inset = size * 0.06f + stroke / 2;
            var face = new RectangleF(inset, inset, size - 2 * inset, size - 2 * inset);

            using var faceBrush = new SolidBrush(Color.White);
            using var ringPen = new Pen(color, stroke);
            using var handPen = new Pen(color, stroke * 0.85f) { StartCap = LineCap.Round, EndCap = LineCap.Round };

            graphics.FillEllipse(faceBrush, face);
            graphics.DrawEllipse(ringPen, face);

            var center = new PointF(size / 2f, size / 2f);
            graphics.DrawLine(handPen, center, new PointF(center.X, center.Y - face.Height * 0.30f));
            graphics.DrawLine(handPen, center, new PointF(center.X + face.Width * 0.24f, center.Y));
        }

        IntPtr handle = bitmap.GetHicon();
        try
        {
            // Kopyası kendi tanıtıcısına sahip olur; geçici tanıtıcı hemen serbest bırakılır.
            using var temporary = Icon.FromHandle(handle);
            return (Icon)temporary.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
