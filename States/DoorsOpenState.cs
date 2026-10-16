using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>Doors fully open; waits <see cref="ElevatorSettings.DoorDwellMs"/> before closing.</summary>
public sealed class DoorsOpenState : IElevatorState
{
    public static DoorsOpenState Instance { get; } = new();

    private DoorsOpenState()
    {
    }

    public string Name => "Doors open";

    public void Enter(Elevator elevator) => elevator.DwellElapsedMs = 0;

    public void HandleRequest(Elevator elevator, int floor, RequestSource source)
    {
        if (floor == elevator.CurrentFloor)
        {
            // Already here with the doors open: serve it at once and hold the doors a little longer.
            elevator.DwellElapsedMs = 0;
            elevator.Raise(ElevatorEventType.RequestServed, floor,
                $"Requests for floor {floor} served by {elevator.Name} (doors already open)");
            return;
        }

        elevator.AddStop(floor);

        // A floor button pressed inside the car means the passenger is ready to go: close the doors
        // now. A request from another floor waits for the normal dwell time.
        if (source == RequestSource.Car)
        {
            elevator.TransitionTo(DoorsClosingState.Instance);
        }
    }

    public void Tick(Elevator elevator, int elapsedMs)
    {
        elevator.DwellElapsedMs += elapsedMs;
        if (elevator.DwellElapsedMs >= ElevatorSettings.DoorDwellMs)
        {
            elevator.TransitionTo(DoorsClosingState.Instance);
        }
    }

    public double RemainingTaskMs(Elevator elevator) =>
        Math.Max(0, ElevatorSettings.DoorDwellMs - elevator.DwellElapsedMs) + ElevatorSettings.DoorMoveMs;
}
