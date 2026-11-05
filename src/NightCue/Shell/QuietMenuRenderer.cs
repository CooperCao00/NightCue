using System.Drawing;
using System.Windows.Forms;

namespace NightCue
{
    public class QuietMenuRenderer : ToolStripProfessionalRenderer
    {
        readonly bool dark;
        public QuietMenuRenderer(bool value) : base(new MenuColors(value))
        {
            dark = value;
            RoundedEdges = false;
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? (dark ? Color.FromArgb(231, 233, 244) : Color.FromArgb(40, 44, 60)) : (dark ? Color.FromArgb(145, 154, 179) : Color.FromArgb(122, 129, 148));
            base.OnRenderItemText(e);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled)
                return;
            Rectangle r = new Rectangle(3, 1, e.Item.Width - 6, e.Item.Height - 2);
            using (var p = new System.Drawing.Drawing2D.GraphicsPath())
            {
                p.AddArc(r.Left, r.Top, 12, 12, 180, 90);
                p.AddArc(r.Right - 12, r.Top, 12, 12, 270, 90);
                p.AddArc(r.Right - 12, r.Bottom - 12, 12, 12, 0, 90);
                p.AddArc(r.Left, r.Bottom - 12, 12, 12, 90, 90);
                p.CloseFigure();
                using (var b = new SolidBrush(dark ? Color.FromArgb(57, 51, 83) : Color.FromArgb(240, 237, 255)))
                    e.Graphics.FillPath(b, p);
            }
        }

        class MenuColors : ProfessionalColorTable
        {
            readonly bool dark;
            public MenuColors(bool value)
            {
                dark = value;
                UseSystemColors = false;
            }

            public override Color ToolStripDropDownBackground
            {
                get
                {
                    return dark ? Color.FromArgb(34, 39, 56) : Color.White;
                }
            }

            public override Color MenuBorder
            {
                get
                {
                    return dark ? Color.FromArgb(58, 64, 87) : Color.FromArgb(224, 227, 237);
                }
            }

            public override Color SeparatorDark
            {
                get
                {
                    return MenuBorder;
                }
            }

            public override Color SeparatorLight
            {
                get
                {
                    return ToolStripDropDownBackground;
                }
            }
        }
    }
}
