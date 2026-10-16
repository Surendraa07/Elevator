namespace ElevatorSystem.Forms;

/// <summary>
/// The control panel inside one car: a current-floor display and one button per floor.
/// The panel only reports clicks; <see cref="Observers.CarPanelObserver"/> decides what it shows.
/// </summary>
public sealed class CarPanelControl : GroupBox
{
    private readonly Label _floorDisplay;
    private readonly Label _stateDisplay;
    private readonly Dictionary<int, Button> _floorButtons = new();

    public CarPanelControl(int elevatorId, int floorCount)
    {
        Theme theme = Theme.Current;
        Text = $"Elevator {elevatorId} – car panel";
        Size = new Size(180, 140 + floorCount * 70);
        Font = theme.UiFont(9F);
        theme.StylePanel(this);

        _floorDisplay = new Label
        {
            Bounds = new Rectangle(15, 28, 150, 60),
            BackColor = theme.DisplayBack,
            ForeColor = theme.DisplayText,
            Font = new Font("Consolas", 28F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };
        _stateDisplay = new Label
        {
            Bounds = new Rectangle(15, 90, 150, 20),
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.Transparent,
            ForeColor = theme.MutedText
        };
        Controls.Add(_floorDisplay);
        Controls.Add(_stateDisplay);

        // Top floor first, like a real panel.
        for (int floor = floorCount - 1, row = 0; floor >= 0; floor--, row++)
        {
            int buttonFloor = floor;
            var button = new Button
            {
                Bounds = new Rectangle(55, 122 + row * 70, 70, 60),
                Text = buttonFloor.ToString(),
                Font = theme.UiFont(18F, FontStyle.Bold),
                Tag = buttonFloor
            };
            ButtonStyle.Apply(button);
            button.Click += (_, _) => FloorButtonClicked?.Invoke(this, buttonFloor);
            _floorButtons[buttonFloor] = button;
            Controls.Add(button);
        }
    }

    /// <summary>Raised with the floor number when a floor button is pressed.</summary>
    public event EventHandler<int>? FloorButtonClicked;

    public void SetFloorButtonLit(int floor, bool lit)
    {
        if (_floorButtons.TryGetValue(floor, out Button? button))
        {
            ButtonStyle.SetLit(button, lit);
        }
    }

    public void SetDisplay(string floorText, string stateText)
    {
        _floorDisplay.Text = floorText;
        _stateDisplay.Text = stateText;
    }
}
