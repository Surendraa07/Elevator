using ElevatorSystem.Observers;

namespace ElevatorSystem.Models;

/// <summary>
/// Assigns requests made with the floor (hall) buttons to an elevator.
/// <para>
/// Each request goes to the elevator with the shortest estimated response time. The request is appended
/// to that elevator's queue, so its current task is never interrupted. Requests from inside a car never
/// pass through here; that car handles them itself.
/// </para>
/// <para>
/// The dispatcher observes every elevator, so it knows when a waiting call has been served and the
/// floor's request button can reset.
/// </para>
/// </summary>
public sealed class Dispatcher : IElevatorObserver
{
    private readonly IReadOnlyList<Elevator> _elevators;

    // Floor -> elevator assigned to the waiting hall call on that floor.
    private readonly Dictionary<int, Elevator> _hallCalls = new();

    public Dispatcher(IReadOnlyList<Elevator> elevators)
    {
        if (elevators.Count == 0)
        {
            throw new ArgumentException("At least one elevator is required.", nameof(elevators));
        }

        _elevators = elevators;
        foreach (Elevator elevator in elevators)
        {
            elevator.Attach(this);
        }
    }

    /// <summary>Raised when a floor's request button should light up (call waiting) or reset (served).</summary>
    public event EventHandler<HallCallEventArgs>? HallCallChanged;

    public IReadOnlyList<Elevator> Elevators => _elevators;

    public bool IsHallCallActive(int floor) => _hallCalls.ContainsKey(floor);

    /// <summary>Handles a press of the request button on <paramref name="floor"/>.</summary>
    public Elevator HandleHallRequest(int floor)
    {
        // Pressing a lit button again changes nothing: the call already has an elevator.
        if (_hallCalls.TryGetValue(floor, out Elevator? assigned))
        {
            return assigned;
        }

        Elevator best = _elevators[0];
        double bestTime = double.MaxValue;
        foreach (Elevator elevator in _elevators)
        {
            double time = elevator.EstimateResponseTimeMs(floor);
            if (time < bestTime)
            {
                best = elevator;
                bestTime = time;
            }
        }

        _hallCalls[floor] = best;
        HallCallChanged?.Invoke(this, new HallCallEventArgs(floor, true, best));

        string? note = _elevators.Count > 1 ? $"dispatcher ETA {bestTime / 1000:0.0} s" : null;
        best.RequestFloor(floor, RequestSource.Hall, note);
        return best;
    }

    public void Update(Elevator elevator, ElevatorEventArgs e)
    {
        // Any car opening its doors at the floor serves the people waiting there, so the button resets.
        if (e.EventType == ElevatorEventType.RequestServed && _hallCalls.Remove(e.Floor))
        {
            HallCallChanged?.Invoke(this, new HallCallEventArgs(e.Floor, false, elevator));
        }
    }
}
