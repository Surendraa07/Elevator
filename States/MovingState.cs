using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>Car travelling to <see cref="Elevator.TargetFloor"/> with the doors closed.</summary>
public sealed class MovingState : IElevatorState
{
    public static MovingState Instance { get; } = new();

    private MovingState()
    {
    }

    public string Name => "Moving";

    public void Enter(Elevator elevator)
    {
        int target = elevator.TargetFloor!.Value;
        string direction = elevator.Direction == Direction.Up ? "up" : "down";
        elevator.Raise(ElevatorEventType.MoveStarted, elevator.CurrentFloor,
            $"{elevator.Name} moving {direction} from floor {elevator.CurrentFloor} to floor {target}");
    }

    public void HandleRequest(Elevator elevator, int floor, RequestSource source)
    {
        // The car never turns around mid-trip: the request waits in the queue until the current trip
        // ends. A request for the floor being left is served by coming back afterwards.
        elevator.AddStop(floor);
    }

    public void Tick(Elevator elevator, int elapsedMs)
    {
        int target = elevator.TargetFloor!.Value;
        double step = (double)elapsedMs / ElevatorSettings.TravelMsPerFloor;

        elevator.Position = elevator.Position < target
            ? Math.Min(elevator.Position + step, target)
            : Math.Max(elevator.Position - step, target);

        if (elevator.Position == target)
        {
            elevator.CurrentFloor = target;
            elevator.TargetFloor = null;
            elevator.Raise(ElevatorEventType.Arrived, target, $"{elevator.Name} arrived at floor {target}");
            return;
        }

        // Report intermediate floors passed on the way (only happens in buildings with more than 2 floors).
        int reached = elevator.Direction == Direction.Up
            ? (int)Math.Floor(elevator.Position)
            : (int)Math.Ceiling(elevator.Position);
        if (reached != elevator.CurrentFloor)
        {
            elevator.CurrentFloor = reached;
            elevator.Raise(ElevatorEventType.FloorPassed, reached, $"{elevator.Name} passing floor {reached}");
        }
    }

    public double RemainingTaskMs(Elevator elevator) =>
        Math.Abs(elevator.TargetFloor!.Value - elevator.Position) * ElevatorSettings.TravelMsPerFloor
        + ElevatorSettings.DoorMoveMs + ElevatorSettings.DoorDwellMs + ElevatorSettings.DoorMoveMs;
}
