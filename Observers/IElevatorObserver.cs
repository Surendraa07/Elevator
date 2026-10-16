using ElevatorSystem.Models;

namespace ElevatorSystem.Observers;

/// <summary>
/// Observer pattern: anything that reacts to elevator events (displays, the logger, the dispatcher)
/// implements this and registers with <see cref="Elevator.Attach"/>.
/// </summary>
public interface IElevatorObserver
{
    void Update(Elevator elevator, ElevatorEventArgs e);
}
