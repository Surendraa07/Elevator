using ElevatorSystem.Models;

namespace ElevatorSystem.Observers;

/// <summary>
/// The status display mounted on one floor. It observes every elevator and shows where each one is,
/// e.g. "Floor 1" or "▲ to 1". A car standing at this display's floor is marked with "◄".
/// </summary>
public sealed class FloorDisplayObserver : IElevatorObserver
{
    private readonly Label _display;
    private readonly int _floor;
    private readonly IReadOnlyList<Elevator> _elevators;

    public FloorDisplayObserver(Label display, int floor, IReadOnlyList<Elevator> elevators)
    {
        _display = display;
        _floor = floor;
        _elevators = elevators;

        foreach (Elevator elevator in elevators)
        {
            elevator.Attach(this);
        }

        Refresh();
    }

    public void Update(Elevator elevator, ElevatorEventArgs e) => Refresh();

    private void Refresh()
    {
        var lines = _elevators.Select(elevator =>
        {
            string here = elevator.TargetFloor is null && elevator.CurrentFloor == _floor ? " ◄" : string.Empty;
            string prefix = _elevators.Count > 1 ? $"E{elevator.Id}: " : string.Empty;
            return prefix + elevator.DescribeLocation() + here;
        });

        _display.Text = string.Join(Environment.NewLine, lines);
    }
}
