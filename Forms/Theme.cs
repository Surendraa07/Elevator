namespace ElevatorSystem.Forms;

/// <summary>
/// Colours and fonts for the whole UI. <see cref="Current"/> is chosen once at startup (in the mode
/// dialog) and read by every form and control when it is built or painted.
/// </summary>
public sealed class Theme
{
    public static Theme Classic { get; } = new()
    {
        Name = "Classic",
        IsSpace = false,
        UiFontFamily = "Segoe UI",
        FormBack = Color.FromArgb(246, 246, 243),
        PanelBack = Color.FromArgb(246, 246, 243),
        Text = Color.Black,
        MutedText = Color.DimGray,
        HeadingText = Color.Black,
        ButtonBack = Color.FromArgb(225, 228, 232),
        ButtonBorder = Color.FromArgb(150, 155, 160),
        ButtonText = Color.Black,
        LitBack = Color.Gold,
        LitBorder = Color.DarkOrange,
        LitText = Color.Black,
        DisplayBack = Color.Black,
        DisplayText = Color.Lime,
        WallColor = Color.FromArgb(236, 234, 228),
        ShaftColor = Color.FromArgb(58, 62, 70),
        SlabColor = Color.FromArgb(150, 150, 145),
        CarInterior = Color.FromArgb(252, 238, 200),
        CarFrame = Color.FromArgb(90, 95, 105),
        DoorColor = Color.FromArgb(176, 186, 198),
        DoorEdge = Color.FromArgb(120, 128, 140),
        DoorFrame = Color.FromArgb(70, 70, 70),
        ShaftTitle = Color.White,
        Cable = Color.Silver,
        GridBack = SystemColors.Window,
        GridText = SystemColors.ControlText,
        GridLines = SystemColors.ControlDark,
        GridHeaderBack = SystemColors.Control,
        GridSelection = SystemColors.Highlight
    };

    public static Theme SpaceStation { get; } = new()
    {
        Name = "Space station",
        IsSpace = true,
        UiFontFamily = "Bahnschrift",
        FormBack = Color.FromArgb(7, 10, 22),
        // Translucent so the starfield behind the window shows through the panels.
        PanelBack = Color.FromArgb(170, 16, 24, 42),
        Text = Color.FromArgb(205, 222, 240),
        MutedText = Color.FromArgb(120, 150, 180),
        HeadingText = Color.FromArgb(110, 210, 255),
        ButtonBack = Color.FromArgb(30, 40, 60),
        ButtonBorder = Color.FromArgb(60, 120, 170),
        ButtonText = Color.FromArgb(200, 230, 255),
        LitBack = Color.FromArgb(255, 170, 40),
        LitBorder = Color.FromArgb(255, 220, 140),
        LitText = Color.FromArgb(25, 15, 0),
        DisplayBack = Color.FromArgb(2, 8, 16),
        DisplayText = Color.FromArgb(0, 225, 255),
        WallColor = Color.FromArgb(26, 31, 44),
        ShaftColor = Color.FromArgb(10, 14, 28),
        SlabColor = Color.FromArgb(230, 180, 40),
        CarInterior = Color.FromArgb(205, 232, 255),
        CarFrame = Color.FromArgb(140, 160, 185),
        DoorColor = Color.FromArgb(78, 92, 114),
        DoorEdge = Color.FromArgb(130, 150, 175),
        DoorFrame = Color.FromArgb(150, 165, 185),
        ShaftTitle = Color.FromArgb(110, 210, 255),
        Cable = Color.FromArgb(90, 110, 140),
        GridBack = Color.FromArgb(12, 17, 30),
        GridText = Color.FromArgb(200, 220, 240),
        GridLines = Color.FromArgb(40, 55, 80),
        GridHeaderBack = Color.FromArgb(22, 32, 52),
        GridSelection = Color.FromArgb(0, 110, 150)
    };

    /// <summary>The theme in use; set before the main window is created.</summary>
    public static Theme Current { get; set; } = SpaceStation;

    public required string Name { get; init; }

    /// <summary>True for the space station look (starfield, glass tube, airlock doors, hazard stripes).</summary>
    public required bool IsSpace { get; init; }

    public required string UiFontFamily { get; init; }

    public required Color FormBack { get; init; }
    public required Color PanelBack { get; init; }
    public required Color Text { get; init; }
    public required Color MutedText { get; init; }
    public required Color HeadingText { get; init; }

    public required Color ButtonBack { get; init; }
    public required Color ButtonBorder { get; init; }
    public required Color ButtonText { get; init; }
    public required Color LitBack { get; init; }
    public required Color LitBorder { get; init; }
    public required Color LitText { get; init; }

    public required Color DisplayBack { get; init; }
    public required Color DisplayText { get; init; }

    public required Color WallColor { get; init; }
    public required Color ShaftColor { get; init; }
    public required Color SlabColor { get; init; }
    public required Color CarInterior { get; init; }
    public required Color CarFrame { get; init; }
    public required Color DoorColor { get; init; }
    public required Color DoorEdge { get; init; }
    public required Color DoorFrame { get; init; }
    public required Color ShaftTitle { get; init; }
    public required Color Cable { get; init; }

    public required Color GridBack { get; init; }
    public required Color GridText { get; init; }
    public required Color GridLines { get; init; }
    public required Color GridHeaderBack { get; init; }
    public required Color GridSelection { get; init; }

    public Font UiFont(float size, FontStyle style = FontStyle.Regular) => new(UiFontFamily, size, style);

    /// <summary>Applies the standard colours to a GroupBox used as a panel.</summary>
    public void StylePanel(GroupBox group)
    {
        group.BackColor = PanelBack;
        group.ForeColor = HeadingText;
    }
}
