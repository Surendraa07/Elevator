using System.Drawing.Drawing2D;
using ElevatorSystem.Models;

namespace ElevatorSystem.Forms;

/// <summary>
/// Draws one elevator: the shaft with the moving car and its sliding car doors (right), and the floor door
/// at each landing (left). The car door and the floor door of the floor the car stands at open by the
/// same amount, because both are drawn from <see cref="Elevator.DoorOpenRatio"/>.
/// Repaints whenever the elevator raises <see cref="Elevator.Animated"/>.
/// <para>
/// With the space station theme the shaft becomes a glass tube looking out into space, the landings
/// become hull plating with hazard-striped decks, and the doors become airlocks with status lights.
/// </para>
/// </summary>
public sealed class ElevatorShaftControl : Control
{
    private const int Pad = 10;
    private const int LandingWidth = 72;
    private const int SlabThickness = 6;

    private readonly Elevator _elevator;
    private readonly Theme _theme = Theme.Current;

    // Starfield seen through the glass tube; regenerated only when the control is resized.
    private Bitmap? _stars;

    public ElevatorShaftControl(Elevator elevator)
    {
        _elevator = elevator;
        DoubleBuffered = true;
        ResizeRedraw = true;
        _elevator.Animated += (_, _) => Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _stars?.Dispose();
        _stars = null;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(_theme.WallColor);

        int floorCount = _elevator.FloorCount;
        float floorHeight = (Height - 2f * Pad) / floorCount;
        var shaft = new RectangleF(LandingWidth + Pad, Pad, Width - LandingWidth - 2 * Pad, Height - 2 * Pad);

        if (_theme.IsSpace)
        {
            DrawHullPlating(g);
            DrawGlassTube(g, shaft, floorHeight);
        }
        else
        {
            using var shaftBrush = new SolidBrush(_theme.ShaftColor);
            g.FillRectangle(shaftBrush, shaft);
        }

        float carHeight = floorHeight - 34;
        float doorWidth = LandingWidth - 22;

        using var titleFont = _theme.UiFont(9F, FontStyle.Bold);
        using var smallFont = _theme.UiFont(7.5F);
        using var titleBrush = new SolidBrush(_theme.ShaftTitle);
        using var labelBrush = new SolidBrush(_theme.MutedText);
        string title = _theme.IsSpace ? _elevator.Name.ToUpperInvariant() : _elevator.Name;
        g.DrawString(title, titleFont, titleBrush, shaft.X + 6, shaft.Y + 4);

        for (int floor = 0; floor < floorCount; floor++)
        {
            // Floor level (top of the slab) for this floor; floor 0 is at the bottom.
            float levelY = Pad + (floorCount - floor) * floorHeight - SlabThickness;
            DrawSlab(g, new RectangleF(0, levelY, Width, SlabThickness));

            // The floor door only opens when the car is standing at this floor.
            bool carHere = _elevator.TargetFloor is null && _elevator.CurrentFloor == floor;
            double open = carHere ? _elevator.DoorOpenRatio : 0;
            var doorFrame = new RectangleF(12, levelY - carHeight, doorWidth, carHeight);
            DrawDoorway(g, doorFrame, open, carHere ? _theme.CarInterior : _theme.ShaftColor);
            g.DrawString($"Floor {floor} door", smallFont, labelBrush, 6, levelY - carHeight - 16);
        }

        // Hoist cable from the top of the shaft to the car.
        float carBottom = (float)(Pad + (floorCount - _elevator.Position) * floorHeight - SlabThickness);
        var car = new RectangleF(shaft.X + 14, carBottom - carHeight, shaft.Width - 28, carHeight);
        using (var cable = new Pen(_theme.Cable, 2))
        {
            g.DrawLine(cable, car.X + car.Width / 2, shaft.Y, car.X + car.Width / 2, car.Y);
        }

        if (_theme.IsSpace)
        {
            // Soft light spilling around the car, as if its cabin lights reflect off the tube.
            using var glowPath = new GraphicsPath();
            glowPath.AddRectangle(RectangleF.Inflate(car, 12, 12));
            using var glow = new PathGradientBrush(glowPath)
            {
                CenterColor = Color.FromArgb(50, 120, 200, 255),
                SurroundColors = new[] { Color.Transparent }
            };
            g.FillPath(glow, glowPath);
        }

        DrawDoorway(g, car, _elevator.DoorOpenRatio, _theme.CarInterior);
        using var carPen = new Pen(_theme.CarFrame, 3);
        g.DrawRectangle(carPen, car.X, car.Y, car.Width, car.Height);
    }

    /// <summary>Space theme: the landing column as riveted hull panels.</summary>
    private void DrawHullPlating(Graphics g)
    {
        using var seam = new Pen(Color.FromArgb(44, 52, 70), 1);
        using var rivet = new SolidBrush(Color.FromArgb(70, 82, 104));
        float right = LandingWidth + Pad;

        for (int y = 0; y < Height; y += 40)
        {
            g.DrawLine(seam, 0, y, right, y);
            for (int x = 6; x < right - 2; x += 16)
            {
                g.FillEllipse(rivet, x, y + 3, 2.5f, 2.5f);
            }
        }

        g.DrawLine(seam, right / 2, 0, right / 2, Height);
    }

    /// <summary>Space theme: the shaft as a glass tube with stars behind it and bracing rings.</summary>
    private void DrawGlassTube(Graphics g, RectangleF shaft, float floorHeight)
    {
        _stars ??= SpaceBackdrop.CreateStarfield(Size.Round(shaft.Size), 100 + _elevator.Id, withPlanet: false);
        g.DrawImage(_stars, shaft.X, shaft.Y);

        using (var tint = new LinearGradientBrush(shaft, Color.FromArgb(60, 80, 160, 220), Color.FromArgb(10, 80, 160, 220), LinearGradientMode.Horizontal))
        {
            g.FillRectangle(tint, shaft);
        }

        // Bracing rings every half floor.
        using var ring = new Pen(Color.FromArgb(70, 140, 180, 220), 3);
        for (float y = shaft.Y + floorHeight / 2; y < shaft.Bottom; y += floorHeight / 2)
        {
            g.DrawLine(ring, shaft.X, y, shaft.Right, y);
        }

        // Highlights along the glass edges.
        using var edge = new Pen(Color.FromArgb(150, 120, 210, 255), 2);
        g.DrawLine(edge, shaft.X + 1, shaft.Y, shaft.X + 1, shaft.Bottom);
        g.DrawLine(edge, shaft.Right - 1, shaft.Y, shaft.Right - 1, shaft.Bottom);
        using var shine = new Pen(Color.FromArgb(40, 255, 255, 255), 6);
        g.DrawLine(shine, shaft.X + 9, shaft.Y, shaft.X + 9, shaft.Bottom);
    }

    private void DrawSlab(Graphics g, RectangleF slab)
    {
        if (_theme.IsSpace)
        {
            using var hazard = new HatchBrush(HatchStyle.WideUpwardDiagonal, _theme.SlabColor, Color.FromArgb(20, 20, 24));
            g.FillRectangle(hazard, slab);
        }
        else
        {
            using var brush = new SolidBrush(_theme.SlabColor);
            g.FillRectangle(brush, slab);
        }
    }

    /// <summary>Draws a door frame with two panels that slide apart from the middle.</summary>
    private void DrawDoorway(Graphics g, RectangleF frame, double openRatio, Color behind)
    {
        using (var backBrush = new SolidBrush(behind))
        {
            g.FillRectangle(backBrush, frame);
        }

        float panelWidth = (float)(frame.Width / 2 * (1 - openRatio));
        using var doorBrush = new SolidBrush(_theme.DoorColor);
        using var edgePen = new Pen(_theme.DoorEdge, 1);

        if (panelWidth > 0.5f)
        {
            var left = new RectangleF(frame.X, frame.Y, panelWidth, frame.Height);
            var right = new RectangleF(frame.Right - panelWidth, frame.Y, panelWidth, frame.Height);
            g.FillRectangle(doorBrush, left);
            g.FillRectangle(doorBrush, right);

            if (_theme.IsSpace)
            {
                DrawAirlockDetail(g, left, isLeft: true);
                DrawAirlockDetail(g, right, isLeft: false);
            }

            g.DrawRectangle(edgePen, left.X, left.Y, left.Width, left.Height);
            g.DrawRectangle(edgePen, right.X, right.Y, right.Width, right.Height);
        }

        using var framePen = new Pen(_theme.DoorFrame, 2);
        g.DrawRectangle(framePen, frame.X, frame.Y, frame.Width, frame.Height);

        if (_theme.IsSpace)
        {
            // Airlock status light above the door: green while open (or opening), red when sealed.
            bool open = openRatio > 0.01;
            Color light = open ? Color.FromArgb(60, 255, 120) : Color.FromArgb(255, 60, 60);
            float cx = frame.X + frame.Width / 2;
            float cy = frame.Y - 5;
            using var halo = new SolidBrush(Color.FromArgb(70, light));
            using var lamp = new SolidBrush(light);
            g.FillEllipse(halo, cx - 6, cy - 6, 12, 12);
            g.FillEllipse(lamp, cx - 3, cy - 3, 6, 6);
        }
    }

    /// <summary>Space theme: a hazard stripe on the leading edge and a window band on each airlock panel.</summary>
    private void DrawAirlockDetail(Graphics g, RectangleF panel, bool isLeft)
    {
        float stripeWidth = Math.Min(6, panel.Width);
        var stripe = new RectangleF(isLeft ? panel.Right - stripeWidth : panel.X, panel.Y, stripeWidth, panel.Height);
        using (var hazard = new HatchBrush(HatchStyle.WideUpwardDiagonal, _theme.SlabColor, Color.FromArgb(20, 20, 24)))
        {
            g.FillRectangle(hazard, stripe);
        }

        float bandWidth = panel.Width - stripeWidth - 6;
        if (bandWidth > 4)
        {
            var band = new RectangleF(isLeft ? panel.X + 3 : stripe.Right + 3, panel.Y + panel.Height * 0.25f, bandWidth, 5);
            using var bandBrush = new SolidBrush(Color.FromArgb(120, 140, 210, 255));
            g.FillRectangle(bandBrush, band);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _stars?.Dispose();
        }

        base.Dispose(disposing);
    }
}
