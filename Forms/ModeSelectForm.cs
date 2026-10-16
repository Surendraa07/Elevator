namespace ElevatorSystem.Forms;

/// <summary>Startup dialog: choose single- or two-elevator mode, and the visual theme.</summary>
public sealed class ModeSelectForm : Form
{
    private readonly RadioButton _singleMode;
    private readonly RadioButton _twoMode;
    private readonly RadioButton _spaceTheme;

    public ModeSelectForm()
    {
        Text = "Elevator Simulator";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(360, 290);
        Font = new Font("Segoe UI", 9.5F);

        // Radio buttons are grouped per container, so mode and theme each get their own GroupBox.
        var modeGroup = new GroupBox
        {
            Text = "Elevator system",
            Bounds = new Rectangle(16, 12, 328, 96)
        };
        _singleMode = new RadioButton
        {
            Text = "Single elevator",
            Bounds = new Rectangle(18, 26, 290, 26)
        };
        _twoMode = new RadioButton
        {
            Text = "Two elevators (with dispatcher)",
            Bounds = new Rectangle(18, 56, 290, 26),
            Checked = true
        };
        modeGroup.Controls.AddRange(new Control[] { _singleMode, _twoMode });

        var themeGroup = new GroupBox
        {
            Text = "Theme",
            Bounds = new Rectangle(16, 118, 328, 96)
        };
        _spaceTheme = new RadioButton
        {
            Text = "Space station",
            Bounds = new Rectangle(18, 26, 290, 26),
            Checked = true
        };
        var classicTheme = new RadioButton
        {
            Text = "Classic office building",
            Bounds = new Rectangle(18, 56, 290, 26)
        };
        themeGroup.Controls.AddRange(new Control[] { _spaceTheme, classicTheme });

        var ok = new Button
        {
            Text = "Start",
            Bounds = new Rectangle(160, 240, 85, 32),
            DialogResult = DialogResult.OK
        };
        var cancel = new Button
        {
            Text = "Exit",
            Bounds = new Rectangle(255, 240, 85, 32),
            DialogResult = DialogResult.Cancel
        };

        Controls.AddRange(new Control[] { modeGroup, themeGroup, ok, cancel });
        AcceptButton = ok;
        CancelButton = cancel;
    }

    /// <summary>1 or 2, depending on the chosen mode.</summary>
    public int ElevatorCount => _singleMode.Checked ? 1 : 2;

    public Theme SelectedTheme => _spaceTheme.Checked ? Theme.SpaceStation : Theme.Classic;
}
