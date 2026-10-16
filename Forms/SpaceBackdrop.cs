using System.Drawing.Drawing2D;

namespace ElevatorSystem.Forms;

/// <summary>
/// Paints the space station scenery: a starfield with faint nebulae, optionally with a planet.
/// The bitmaps are generated once from a fixed seed, so the scene looks the same on every run.
/// </summary>
public static class SpaceBackdrop
{
    public static Bitmap CreateStarfield(Size size, int seed, bool withPlanet)
    {
        var bitmap = new Bitmap(Math.Max(1, size.Width), Math.Max(1, size.Height));
        using Graphics g = Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var random = new Random(seed);
        var bounds = new Rectangle(Point.Empty, bitmap.Size);

        using (var sky = new LinearGradientBrush(bounds, Color.FromArgb(4, 6, 16), Color.FromArgb(12, 14, 34), LinearGradientMode.Vertical))
        {
            g.FillRectangle(sky, bounds);
        }

        // Nebulae: large soft glows fading to transparent at their edges.
        Color[] nebulaColors = { Color.FromArgb(60, 90, 40, 140), Color.FromArgb(50, 0, 120, 140), Color.FromArgb(40, 140, 40, 90) };
        for (int i = 0; i < 3; i++)
        {
            float w = bitmap.Width * (0.4f + (float)random.NextDouble() * 0.5f);
            float h = bitmap.Height * (0.3f + (float)random.NextDouble() * 0.4f);
            var area = new RectangleF((float)random.NextDouble() * bitmap.Width - w / 2, (float)random.NextDouble() * bitmap.Height - h / 2, w, h);
            using var path = new GraphicsPath();
            path.AddEllipse(area);
            using var glow = new PathGradientBrush(path)
            {
                CenterColor = nebulaColors[i % nebulaColors.Length],
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(glow, path);
        }

        // Stars: mostly tiny and dim, a few bright ones with a cross-shaped glint.
        int starCount = bitmap.Width * bitmap.Height / 900;
        for (int i = 0; i < starCount; i++)
        {
            float x = (float)random.NextDouble() * bitmap.Width;
            float y = (float)random.NextDouble() * bitmap.Height;
            int brightness = 90 + random.Next(166);
            int tint = random.Next(3);
            Color color = tint switch
            {
                0 => Color.FromArgb(brightness, 200, 220, 255),
                1 => Color.FromArgb(brightness, 255, 240, 210),
                _ => Color.FromArgb(brightness, 255, 255, 255)
            };
            float radius = random.NextDouble() < 0.08 ? 1.6f : 0.7f;
            using var star = new SolidBrush(color);
            g.FillEllipse(star, x - radius, y - radius, radius * 2, radius * 2);

            if (radius > 1f && random.NextDouble() < 0.5)
            {
                using var glint = new Pen(Color.FromArgb(brightness / 3, color), 1f);
                g.DrawLine(glint, x - 5, y, x + 5, y);
                g.DrawLine(glint, x, y - 5, x, y + 5);
            }
        }

        if (withPlanet)
        {
            DrawPlanet(g, bitmap.Size);
        }

        return bitmap;
    }

    /// <summary>A large planet rising from the bottom-right corner, lit from the upper left.</summary>
    private static void DrawPlanet(Graphics g, Size size)
    {
        float diameter = Math.Max(size.Width, size.Height) * 0.55f;
        var body = new RectangleF(size.Width - diameter * 0.55f, size.Height - diameter * 0.38f, diameter, diameter);

        // Atmosphere halo.
        var halo = RectangleF.Inflate(body, diameter * 0.05f, diameter * 0.05f);
        using (var haloPath = new GraphicsPath())
        {
            haloPath.AddEllipse(halo);
            using var haloBrush = new PathGradientBrush(haloPath)
            {
                CenterColor = Color.FromArgb(0, 80, 180, 255),
                SurroundColors = new[] { Color.Transparent },
                FocusScales = new PointF(0.9f, 0.9f)
            };
            g.FillPath(haloBrush, haloPath);
        }

        using (var surface = new LinearGradientBrush(body, Color.FromArgb(70, 150, 210), Color.FromArgb(10, 30, 60), 45f))
        {
            g.FillEllipse(surface, body);
        }

        // Cloud bands, clipped to the planet.
        using (var clip = new GraphicsPath())
        {
            clip.AddEllipse(body);
            GraphicsState state = g.Save();
            g.SetClip(clip);
            using var band = new Pen(Color.FromArgb(40, 220, 240, 255), diameter * 0.03f);
            for (int i = 1; i < 6; i++)
            {
                float y = body.Y + body.Height * i / 6f;
                g.DrawArc(band, body.X - diameter * 0.2f, y - diameter * 0.1f, diameter * 1.4f, diameter * 0.25f, 200, 140);
            }

            // Night side: darken the lower-right part.
            using var shadow = new LinearGradientBrush(body, Color.Transparent, Color.FromArgb(200, 2, 4, 12), 45f);
            g.FillEllipse(shadow, body);
            g.Restore(state);
        }
    }
}
