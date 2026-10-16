using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>Car door and floor door sliding shut together before the car may move.</summary>
public sealed class DoorsClosingState : IElevatorState
{
    public static DoorsClosingState Instance { get; } = new();

    private DoorsClosingState()
    {
    }

    public string Name => "Doors closing";

    public void Enter(Elevator elevator)
    {
        elevator.Raise(ElevatorEventType.DoorsClosing, elevator.CurrentFloor,
            $"Car and floor doors closing at floor {elevator.CurrentFloor}");
    }

    public void HandleRequest(Elevator elevator, int floor, RequestSource source)
    {
        if (floor == elevator.CurrentFloor)
        {
            // Someone at this floor wants the car: reopen from where the doors are, like a door sensor would.
            elevator.TransitionTo(DoorsOpeningState.Instance);
            return;
        }

        elevator.AddStop(floor);
    }

    public void Tick(Elevator elevator, int elapsedMs)
    {
        elevator.DoorOpenRatio = Math.Max(0.0, elevator.DoorOpenRatio - (double)elapsedMs / ElevatorSettings.DoorMoveMs);
        if (elevator.DoorOpenRatio <= 0.0)
        {
            // The elevator's DoorsClosed handler starts the next queued stop, or goes idle.
            elevator.Raise(ElevatorEventType.DoorsClosed, elevator.CurrentFloor,
                $"Car and floor doors closed at floor {elevator.CurrentFloor}");
        }
    }

    public double RemainingTaskMs(Elevator elevator) => elevator.DoorOpenRatio * ElevatorSettings.DoorMoveMs;
}
