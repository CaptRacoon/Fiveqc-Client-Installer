using System.Drawing.Drawing2D;

namespace FiveQC.ClientInstaller;

internal static class Theme
{
    public static readonly Color Background = Color.FromArgb(20, 22, 25);
    public static readonly Color Surface = Color.FromArgb(31, 34, 38);
    public static readonly Color SurfaceAlt = Color.FromArgb(39, 42, 47);
    public static readonly Color Border = Color.FromArgb(65, 69, 75);
    public static readonly Color Accent = Color.FromArgb(226, 109, 38);
    public static readonly Color AccentHover = Color.FromArgb(242, 129, 55);
    public static readonly Color Text = Color.FromArgb(238, 239, 241);
    public static readonly Color MutedText = Color.FromArgb(170, 175, 181);
    public static readonly Color Success = Color.FromArgb(76, 176, 112);

    public static Button CreateButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Accent : SurfaceAlt,
            ForeColor = Text,
            Font = new Font("Segoe UI Semibold", 9.5f),
            Cursor = Cursors.Hand,
            Padding = new Padding(12, 0, 12, 0),
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        button.FlatAppearance.BorderColor = Border;
        button.MouseEnter += (_, _) => button.BackColor = primary ? AccentHover : Color.FromArgb(49, 53, 59);
        button.MouseLeave += (_, _) => button.BackColor = primary ? Accent : SurfaceAlt;
        return button;
    }
}

internal sealed class HeaderPanel : Panel
{
    public Image? BannerImage { get; set; }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

        if (BannerImage is not null)
        {
            Rectangle destination = ClientRectangle;
            float imageRatio = (float)BannerImage.Width / BannerImage.Height;
            float targetRatio = (float)Math.Max(1, Width) / Math.Max(1, Height);
            Rectangle source;
            if (imageRatio > targetRatio)
            {
                int sourceWidth = (int)(BannerImage.Height * targetRatio);
                source = new Rectangle((BannerImage.Width - sourceWidth) / 2, 0, sourceWidth, BannerImage.Height);
            }
            else
            {
                int sourceHeight = (int)(BannerImage.Width / targetRatio);
                source = new Rectangle(0, (BannerImage.Height - sourceHeight) / 2, BannerImage.Width, sourceHeight);
            }
            e.Graphics.DrawImage(BannerImage, destination, source, GraphicsUnit.Pixel);
        }

        using var overlay = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(220, 15, 17, 20),
            Color.FromArgb(75, 15, 17, 20),
            LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(overlay, ClientRectangle);

        using var bottomShade = new LinearGradientBrush(
            ClientRectangle,
            Color.FromArgb(0, 20, 22, 25),
            Color.FromArgb(245, 20, 22, 25),
            LinearGradientMode.Vertical);
        e.Graphics.FillRectangle(bottomShade, ClientRectangle);
    }
}
