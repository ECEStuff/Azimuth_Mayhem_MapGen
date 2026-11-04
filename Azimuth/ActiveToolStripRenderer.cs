// Draws the checked state of map toolbar buttons that toggle persistent display options.

using System.Drawing;
using System.Windows.Forms;

namespace Azimuth
{
    internal sealed class ActiveToolStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderButtonBackground(
            ToolStripItemRenderEventArgs e)
        {
            if (e.Item is ToolStripButton button && button.Checked)
            {
                Rectangle rect = new Rectangle(Point.Empty, button.Size);
                rect.Inflate(-1, -1);

                using (SolidBrush brush =
                    new SolidBrush(Color.FromArgb(215, 232, 250)))
                using (Pen border = new Pen(Color.SteelBlue))
                {
                    e.Graphics.FillRectangle(brush, rect);
                    e.Graphics.DrawRectangle(
                        border,
                        rect.X,
                        rect.Y,
                        rect.Width - 1,
                        rect.Height - 1);
                }
            }
            else
            {
                base.OnRenderButtonBackground(e);
            }
        }
    }
}
