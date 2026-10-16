using System.Diagnostics;
using ElevatorSystem.Observers;
using ElevatorSystem.States;

namespace ElevatorSystem.Models;

/// <summary>
/// One elevator: car position, doors, the queue of floors it still has to visit and its current state.
/// <para>
/// State pattern: behaviour that depends on what the car is doing (handling a request, advancing an
/// animation step) is delegated to <see cref="State"/>.
/// </para>
/// <para>
/// Observer pattern: displays, the logger and the dispatcher register through <see cref="Attach"/>
/// and receive every <see cref="ElevatorEventArgs"/>.
/// </para>
/// <para>
/// Sequencing: each finished step raises a custom event (ElevatorArrived, DoorsOpened, DoorsClosed) and
/// the elevator starts the next step from its own handler for that event. A WinForms timer drives the
/// animation, so nothing blocks the UI thread.
/// </para>
/// </summary>
public sealed class Elevator : IDisposable
{
    private readonly List<int> _stops = new();
    private readonly List<IElevatorObserver> _observers = new();
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _clock = new();
    private long _lastTickMs;

    public Elevator(int id, int floorCount = ElevatorSettings.FloorCount, int startFloor = 0)
    {
        Id = id;
        FloorCount = floorCount;
        CurrentFloor = startFloor;
        Position = startFloor;
        State = IdleState.Instance;

        _timer = new System.Windows.Forms.Timer { Interval = ElevatorSettings.TickMs };
        _timer.Tick += OnTimerTick;

        // Wire up the step sequence: arrival -> open doors -> (dwell) -> close doors -> next stop.
        ElevatorArrived += OnElevatorArrived;
        DoorsOpened += OnDoorsOpened;
        DoorsClosed += OnDoorsClosed;
    }

    /// <summary>Raised when the car stops at its target floor.</summary>
    public event ElevatorEventHandler? ElevatorArrived;

    /// <summary>Raised when the car and floor doors are fully open.</summary>
    public event ElevatorEventHandler? DoorsOpened;

    /// <summary>Raised when the car and floor doors are fully closed.</summary>
    public event ElevatorEventHandler? DoorsClosed;

    /// <summary>Raised when the car starts moving towards a floor.</summary>
    public event ElevatorEventHandler? MoveStarted;

    /// <summary>Raised after every animation step so views can repaint.</summary>
    public event EventHandler? Animated;

    public int Id { get; }
    public string Name => $"Elevator {Id}";
    public int FloorCount { get; }

    /// <summary>The last floor the car stopped at or passed.</summary>
    public int CurrentFloor { get; internal set; }

    /// <summary>Car height in floors (0.0 = floor 0, 1.0 = floor 1). Fractional while moving.</summary>
    public double Position { get; internal set; }

    /// <summary>0 = doors closed, 1 = doors fully open. Car door and floor door always share this value.</summary>
    public double DoorOpenRatio { get; internal set; }

    /// <summary>Floor the car is currently travelling to, or null when it is not moving.</summary>
    public int? TargetFloor { get; internal set; }

    public Direction Direction { get; internal set; }

    /// <summary>How long the doors have been fully open (used by <see cref="DoorsOpenState"/>).</summary>
    internal int DwellElapsedMs { get; set; }

    public IElevatorState State { get; private set; }

    /// <summary>Floors still to visit after the current task, in the order they will be served.</summary>
    public IReadOnlyList<int> PendingStops => _stops;

    public void Attach(IElevatorObserver observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Detach(IElevatorObserver observer) => _observers.Remove(observer);

    /// <summary>
    /// Entry point for all requests. Hall requests arrive here through the dispatcher; car requests come
    /// straight from this car's control panel.
    /// </summary>
    public void RequestFloor(int floor, RequestSource source, string? note = null)
    {
        if (floor < 0 || floor >= FloorCount)
        {
            throw new ArgumentOutOfRangeException(nameof(floor), floor, $"Floor must be between 0 and {FloorCount - 1}.");
        }

        string message = source == RequestSource.Hall
            ? $"Request button pressed at floor {floor}, served by {Name}"
            : $"Floor button {floor} pressed inside {Name}";
        if (note != null)
        {
            message += $" ({note})";
        }

        Raise(source == RequestSource.Hall ? ElevatorEventType.HallRequest : ElevatorEventType.CarRequest, floor, message);
        State.HandleRequest(this, floor, source);
    }

    /// <summary>True when the floor is already the current target or waiting in the queue.</summary>
    public bool HasStop(int floor) => TargetFloor == floor || _stops.Contains(floor);

    /// <summary>
    /// Estimated time (ms) until this elevator could open its doors at <paramref name="floor"/>, assuming
    /// the request is appended to the end of its queue. The current task is never interrupted.
    /// </summary>
    public double EstimateResponseTimeMs(int floor)
    {
        const double doorCycleMs = ElevatorSettings.DoorMoveMs + ElevatorSettings.DoorDwellMs + ElevatorSettings.DoorMoveMs;

        // Already standing at the floor with the doors open, or able to reopen them.
        if (TargetFloor is null && floor == CurrentFloor)
        {
            if (State is DoorsOpenState or DoorsOpeningState)
            {
                return 0;
            }

            if (State is DoorsClosingState)
            {
                return DoorOpenRatio * ElevatorSettings.DoorMoveMs;
            }
        }

        if (TargetFloor == floor)
        {
            return Math.Abs(floor - Position) * ElevatorSettings.TravelMsPerFloor + ElevatorSettings.DoorMoveMs;
        }

        // Finish the current task, then walk the queue in order.
        double time = State.RemainingTaskMs(this);
        int position = TargetFloor ?? CurrentFloor;
        foreach (int stop in _stops)
        {
            time += Math.Abs(stop - position) * ElevatorSettings.TravelMsPerFloor;
            if (stop == floor)
            {
                return time + ElevatorSettings.DoorMoveMs;
            }

            time += doorCycleMs;
            position = stop;
        }

        return time + Math.Abs(floor - position) * ElevatorSettings.TravelMsPerFloor + ElevatorSettings.DoorMoveMs;
    }

    /// <summary>Short human-readable location for the floor displays, e.g. "Floor 1", "▲ going to 1", "Floor 0 open".</summary>
    public string DescribeLocation()
    {
        if (TargetFloor is int target)
        {
            return $"{(Direction == Direction.Up ? "▲" : "▼")} going to {target}";
        }

        string doors = State switch
        {
            DoorsOpeningState => " opening",
            DoorsOpenState => " open",
            DoorsClosingState => " closing",
            _ => string.Empty
        };
        return $"Floor {CurrentFloor}{doors}";
    }

    /// <summary>Queues a stop unless it is already planned. Returns false when it was already planned.</summary>
    internal bool AddStop(int floor)
    {
        if (HasStop(floor))
        {
            return false;
        }

        _stops.Add(floor);
        return true;
    }

    internal void TransitionTo(IElevatorState next)
    {
        State = next;

        // The timer only runs while something is animating; an idle elevator costs nothing.
        if (next is IdleState)
        {
            _timer.Stop();
        }
        else if (!_timer.Enabled)
        {
            _clock.Restart();
            _lastTickMs = 0;
            _timer.Start();
        }

        Raise(ElevatorEventType.StateChanged, CurrentFloor, $"{Name} state -> {next.Name}");

        // Enter may itself transition again (e.g. Idle -> Moving), so nothing may follow that depends on `next`.
        next.Enter(this);
        Animated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Takes the next queued floor and starts serving it, or goes idle when the queue is empty.</summary>
    internal void StartNextStop()
    {
        if (_stops.Count == 0)
        {
            TransitionTo(IdleState.Instance);
            return;
        }

        int next = _stops[0];
        _stops.RemoveAt(0);

        if (next == CurrentFloor)
        {
            TransitionTo(DoorsOpeningState.Instance);
            return;
        }

        TargetFloor = next;
        Direction = next > CurrentFloor ? Direction.Up : Direction.Down;
        TransitionTo(MovingState.Instance);
    }

    /// <summary>
    /// Publishes an event: first to every observer (so the log order matches what happened),
    /// then to the matching custom event, whose handlers may start the next step.
    /// </summary>
    internal void Raise(ElevatorEventType type, int floor, string message)
    {
        var args = new ElevatorEventArgs(Id, type, floor, message, State.Name);

        foreach (IElevatorObserver observer in _observers.ToArray())
        {
            observer.Update(this, args);
        }

        ElevatorEventHandler? handler = type switch
        {
            ElevatorEventType.Arrived => ElevatorArrived,
            ElevatorEventType.DoorsOpened => DoorsOpened,
            ElevatorEventType.DoorsClosed => DoorsClosed,
            ElevatorEventType.MoveStarted => MoveStarted,
            _ => null
        };
        handler?.Invoke(this, args);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        // Use real elapsed time: WinForms timers are not precise, and a long pause (e.g. a modal
        // dialog) should not make the car jump, hence the clamp.
        long now = _clock.ElapsedMilliseconds;
        int elapsed = (int)Math.Clamp(now - _lastTickMs, 1, 100);
        _lastTickMs = now;

        State.Tick(this, elapsed);
        Animated?.Invoke(this, EventArgs.Empty);
    }

    private void OnElevatorArrived(Elevator sender, ElevatorEventArgs e) => TransitionTo(DoorsOpeningState.Instance);

    private void OnDoorsOpened(Elevator sender, ElevatorEventArgs e)
    {
        // Anyone waiting for this floor is served now, so it no longer needs a queued stop.
        _stops.RemoveAll(f => f == CurrentFloor);
        TransitionTo(DoorsOpenState.Instance);
        Raise(ElevatorEventType.RequestServed, CurrentFloor, $"Requests for floor {CurrentFloor} served by {Name}");
    }

    private void OnDoorsClosed(Elevator sender, ElevatorEventArgs e) => StartNextStop();

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
