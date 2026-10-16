using ElevatorSystem.Models;

namespace ElevatorSystem.States;

/// <summary>
/// State pattern: one implementation per phase of the elevator's work cycle
/// (Idle -> Moving -> DoorsOpening -> DoorsOpen -> DoorsClosing -> Idle or Moving).
/// States are stateless singletons; per-elevator data (position, door ratio, dwell time) lives on
/// <see cref="Elevator"/>, so one instance can serve every elevator.
/// </summary>
public interface IElevatorState
{
    /// <summary>Display name, also written to the log.</summary>
    string Name { get; }

    /// <summary>Called once when the elevator enters this state.</summary>
    void Enter(Elevator elevator);

    /// <summary>Handles a request for <paramref name="floor"/> in a way that suits this state.</summary>
    void HandleRequest(Elevator elevator, int floor, RequestSource source);

    /// <summary>Advances this state's animation by <paramref name="elapsedMs"/> (called by the timer).</summary>
    void Tick(Elevator elevator, int elapsedMs);

    /// <summary>Estimated ms until the current task is finished and the doors are closed (for the dispatcher).</summary>
    double RemainingTaskMs(Elevator elevator);
}
