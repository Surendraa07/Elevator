using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>Car stopped at a floor; car door and floor door sliding open together.</summary>
public sealed class DoorsOpeningState : IElevatorState
{
    public static DoorsOpeningState Instance { get; } = new();

    private DoorsOpeningState()
    {
    }

    public string Name => "Doors opening";

    public void Enter(Elevator elevator)
    {
        elevator.Raise(ElevatorEventType.DoorsOpening, elevator.CurrentFloor,
            $"Car and floor doors opening at floor {elevator.CurrentFloor}");
    }

    public void HandleRequest(Elevator elevator, int floor, RequestSource source)
    {
        // Same floor: the doors are already opening, the request is served when they finish.
        if (floor != elevator.CurrentFloor)
        {
            elevator.AddStop(floor);
        }
    }

    public void Tick(Elevator elevator, int elapsedMs)
    {
        elevator.DoorOpenRatio = Math.Min(1.0, elevator.DoorOpenRatio + (double)elapsedMs / ElevatorSettings.DoorMoveMs);
        if (elevator.DoorOpenRatio >= 1.0)
        {
            // The elevator's DoorsOpened handler moves on to DoorsOpenState.
            elevator.Raise(ElevatorEventType.DoorsOpened, elevator.CurrentFloor,
                $"Car and floor doors open at floor {elevator.CurrentFloor}");
        }
    }

    public double RemainingTaskMs(Elevator elevator) =>
        (1.0 - elevator.DoorOpenRatio) * ElevatorSettings.DoorMoveMs
        + ElevatorSettings.DoorDwellMs + ElevatorSettings.DoorMoveMs;
}
