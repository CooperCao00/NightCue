using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NightCue
{
    public static class TrayIcon
    {
        public static Icon CreateSun(Color color)
        {
            using (Bitmap bitmap = new Bitmap(64, 64))
            using (Graphics graphics = Graphics.FromImage(bitmap))
            using (SolidBrush brush = new SolidBrush(color))
            using (Pen pen = new Pen(color, 4))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.FillEllipse(brush, 19, 19, 26, 26);
                for (int i = 0; i < 8; i++)
                {
                    double angle = i * Math.PI / 4;
                    graphics.DrawLine(pen, 32 + (float)Math.Cos(angle) * 20, 32 + (float)Math.Sin(angle) * 20,
                        32 + (float)Math.Cos(angle) * 28, 32 + (float)Math.Sin(angle) * 28);
                }
                IntPtr handle = bitmap.GetHicon();
                try { return (Icon)Icon.FromHandle(handle).Clone(); }
                finally { Native.DestroyIcon(handle); }
            }
        }

        public static Icon Create(Color color, bool moon)
        {
            using (Bitmap b = new Bitmap(64, 64))
            using (Graphics g = Graphics.FromImage(b))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                using (SolidBrush brush = new SolidBrush(color))
                {
                    if (moon)
                    {
                        using (GraphicsPath outer = new GraphicsPath())
                        using (GraphicsPath cut = new GraphicsPath())
                        {
                            outer.AddEllipse(9, 7, 46, 49);
                            cut.AddEllipse(26, 0, 39, 42);
                            using (Region r = new Region(outer))
                            {
                                r.Exclude(cut);
                                g.FillRegion(brush, r);
                            }
                        }

                        g.FillEllipse(brush, 46, 13, 5, 5);
                    }
                    else
                    {
                        using (Pen p = new Pen(color, 5))
                        {
                            g.DrawEllipse(p, 16, 16, 32, 32);
                        }
                    }
                }

                IntPtr h = b.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(h).Clone();
                }
                finally
                {
                    Native.DestroyIcon(h);
                }
            }
        }

    }
}
