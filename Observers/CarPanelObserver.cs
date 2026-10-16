using ElevatorSystem.Forms;
using ElevatorSystem.Models;

namespace ElevatorSystem.Observers;

/// <summary>
/// Keeps one car's control panel in sync with its elevator: the current-floor display, and the floor
/// buttons, which light when pressed and reset when the car opens its doors at that floor.
/// </summary>
public sealed class CarPanelObserver : IElevatorObserver
{
    private readonly CarPanelControl _panel;

    public CarPanelObserver(CarPanelControl panel, Elevator elevator)
    {
        _panel = panel;
        elevator.Attach(this);
        ShowPosition(elevator);
    }

    public void Update(Elevator elevator, ElevatorEventArgs e)
    {
        switch (e.EventType)
        {
            case ElevatorEventType.CarRequest:
                _panel.SetFloorButtonLit(e.Floor, true);
                break;
            case ElevatorEventType.RequestServed:
                _panel.SetFloorButtonLit(e.Floor, false);
                break;
        }

        ShowPosition(elevator);
    }

    private void ShowPosition(Elevator elevator)
    {
        string arrow = elevator.Direction switch
        {
            Direction.Up => "▲",
            Direction.Down => "▼",
            _ => " "
        };
        _panel.SetDisplay($"{elevator.CurrentFloor}{arrow}", elevator.State.Name);
    }
}
