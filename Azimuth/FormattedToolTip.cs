// Optional, width-constrained owner-drawn WinForms tooltips. 
// Existing designer-owned ToolTip components retain their lifecycle and localization bindings.
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Azimuth
{
    internal static class FormattedToolTip
    {
        private const int MaxTextWidth = 480;
        private const int HorizontalPadding = 20;
        private const int VerticalPadding = 16;
        private const TextFormatFlags TextFlags = TextFormatFlags.WordBreak
            | TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix;

        internal static void Configure(ToolTip tooltip)
        {
            if (tooltip == null) throw new ArgumentNullException(nameof(tooltip));

            tooltip.AutoPopDelay = 30000;
            tooltip.InitialDelay = 500;
            tooltip.ReshowDelay = 500;
            tooltip.ShowAlways = true;
            tooltip.IsBalloon = false;
            tooltip.OwnerDraw = true;

            tooltip.Popup += (sender, e) =>
            {
                string text = tooltip.GetToolTip(e.AssociatedControl) ?? string.Empty;
                using (var font = new Font("Segoe UI", 11f, FontStyle.Regular))
                {
                    Size measure = TextRenderer.MeasureText(text, font,
                        new Size(MaxTextWidth, int.MaxValue), TextFlags);
                    e.ToolTipSize = new Size(
                        Math.Min(measure.Width, MaxTextWidth) + HorizontalPadding,
                        measure.Height + VerticalPadding);
                }
            };

            tooltip.Draw += (sender, e) =>
            {
                using (var background = new SolidBrush(Color.LightYellow))
                    e.Graphics.FillRectangle(background, e.Bounds);
                using (var border = new Pen(Color.DarkGoldenrod))
                    e.Graphics.DrawRectangle(border,
                        e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1);

                var textArea = new Rectangle(e.Bounds.X + 10, e.Bounds.Y + 8,
                    Math.Max(1, e.Bounds.Width - HorizontalPadding),
                    Math.Max(1, e.Bounds.Height - VerticalPadding));
                using (var font = new Font("Segoe UI", 11f, FontStyle.Regular))
                    TextRenderer.DrawText(e.Graphics, e.ToolTipText, font,
                        textArea, Color.Black, TextFlags);
            };
        }
    }
}
