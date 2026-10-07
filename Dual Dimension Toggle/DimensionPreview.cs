using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace DualDimensionToggle
{
    // Draw these small engineering examples as vectors, keeping text and arrow
    // edges sharp at different DPI settings without shipping bitmap assets.
    internal sealed class DimensionPreview : Control
    {
        private readonly bool toDual;
        internal DimensionPreview(bool dual)
        {
            // This control is an illustration, not a preview of the active draft.
            // The fixed inch value deliberately keeps two decimal places in both
            // modes; actual drawing precision is determined by StyleMap's target.
            toDual = dual;
            Size = new Size(300, 82);
            MinimumSize = Size;
            TabStop = false;
            AccessibleRole = AccessibleRole.Graphic;
            // The label describes the same information without relying on color.
            // The owning form routes picture clicks to the adjacent radio button.
            AccessibleName = dual ? "Example: 1.00 inch to 25.4 millimeters [1.00 inch]"
                : "Example: 25.4 millimeters [1.00 inch] to 1.00 inch";
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            GraphicsState state = e.Graphics.Save();
            // Graphics belongs to the paint event, not this control. Restore our
            // transformations afterward and never dispose the borrowed Graphics.
            try
            {
                // Center a fixed-aspect illustration; layout may give it extra
                // width. Dispose all GDI resources on every paint, including
                // the arrow cap, so repeated resizing cannot leak handles.
                float scale = System.Math.Min(ClientSize.Width / 300f, ClientSize.Height / 82f);
                e.Graphics.TranslateTransform((ClientSize.Width - 300 * scale) / 2, (ClientSize.Height - 82 * scale) / 2);
                e.Graphics.ScaleTransform(scale, scale);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Read each example left to right: source, conversion arrow,
                // destination. Reversing the requested mode swaps the units, not
                // the conversion arrow's direction or the displayed inch value.
                DrawDimension(e.Graphics, 4, !toDual);
                DrawDimension(e.Graphics, 184, toDual);
                using (Pen arrow = new Pen(Color.FromArgb(173, 104, 12), 2.5f))
                using (AdjustableArrowCap tip = new AdjustableArrowCap(4, 4))
                {
                    arrow.CustomEndCap = tip;
                    e.Graphics.DrawLine(arrow, 133, 39, 168, 39);
                }
            }
            finally { e.Graphics.Restore(state); }
        }

        private static void DrawDimension(Graphics g, int x, bool dual)
        {
            Color color = dual ? Color.FromArgb(0, 112, 122) : SystemColors.ControlText;
            using (Pen line = new Pen(color, 1.2f))
            using (Brush ink = new SolidBrush(color))
            using (Brush caption = new SolidBrush(SystemColors.GrayText))
            using (Font number = new Font("Segoe UI", 15, FontStyle.Regular, GraphicsUnit.Pixel))
            using (Font label = new Font("Segoe UI", 10, FontStyle.Regular, GraphicsUnit.Pixel))
            using (StringFormat center = new StringFormat { Alignment = StringAlignment.Center })
            {
                g.DrawString(dual ? "25.4 [1.00]" : "1.00", number, ink, new RectangleF(x, 10, 112, 23), center);
                g.DrawLine(line, x + 4, 35, x + 4, 62);
                g.DrawLine(line, x + 108, 35, x + 108, 62);
                g.DrawLine(line, x + 4, 46, x + 108, 46);
                // Arrowheads point toward the two extension lines.
                g.FillPolygon(ink, new[] { new Point(x + 4, 46), new Point(x + 13, 43), new Point(x + 13, 49) });
                g.FillPolygon(ink, new[] { new Point(x + 108, 46), new Point(x + 99, 43), new Point(x + 99, 49) });
                g.DrawString(dual ? "mm [inch]" : "inch", label, caption, new RectangleF(x, 64, 112, 15), center);
            }
        }
    }
}
