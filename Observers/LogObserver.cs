using ElevatorSystem.Data;
using ElevatorSystem.Models;

namespace ElevatorSystem.Observers;

/// <summary>
/// Turns elevator events into log entries and hands them to the background writer.
/// It never touches the database itself, so the UI thread never waits on I/O.
/// </summary>
public sealed class LogObserver : IElevatorObserver
{
    private readonly BackgroundLogWriter _writer;

    public LogObserver(BackgroundLogWriter writer, IEnumerable<Elevator> elevators)
    {
        _writer = writer;
        foreach (Elevator elevator in elevators)
        {
            elevator.Attach(this);
        }
    }

    public void Update(Elevator elevator, ElevatorEventArgs e)
    {
        // StateChanged duplicates the specific events (DoorsOpening, MoveStarted, ...), so it is not logged.
        if (e.EventType == ElevatorEventType.StateChanged)
        {
            return;
        }

        _writer.Enqueue(new LogEntry(e.Timestamp, e.ElevatorId, e.Floor, $"{e.EventType}: {e.Message}"));
    }
}
