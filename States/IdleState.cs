using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>Car stopped with the doors closed and nothing to do.</summary>
public sealed class IdleState : IElevatorState
{
    public static IdleState Instance { get; } = new();

    private IdleState()
    {
    }

    public string Name => "Idle";

    public void Enter(Elevator elevator)
    {
        elevator.Direction = Direction.None;
        elevator.Raise(ElevatorEventType.Idle, elevator.CurrentFloor, $"{elevator.Name} idle at floor {elevator.CurrentFloor}, doors closed");
    }

    public void HandleRequest(Elevator elevator, int floor, RequestSource source)
    {
        // Request for the floor the car is already on: no travel, just open the doors.
        if (floor == elevator.CurrentFloor)
        {
            elevator.TransitionTo(DoorsOpeningState.Instance);
            return;
        }

        elevator.AddStop(floor);
        elevator.StartNextStop();
    }

    public void Tick(Elevator elevator, int elapsedMs)
    {
        // Nothing animates while idle (the timer is stopped anyway).
    }

    public double RemainingTaskMs(Elevator elevator) => 0;
}
