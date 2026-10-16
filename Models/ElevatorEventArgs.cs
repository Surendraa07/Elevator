namespace ElevatorSystem.Models;

/// <summary>Data published with every elevator event.</summary>
public sealed class ElevatorEventArgs : EventArgs
{
    public ElevatorEventArgs(int elevatorId, ElevatorEventType eventType, int floor, string message, string stateName)
    {
        Timestamp = DateTime.Now;
        ElevatorId = elevatorId;
        EventType = eventType;
        Floor = floor;
        Message = message;
        StateName = stateName;
    }

    public DateTime Timestamp { get; }
    public int ElevatorId { get; }
    public ElevatorEventType EventType { get; }
    public int Floor { get; }
    public string Message { get; }

    /// <summary>Name of the elevator's state at the moment the event was raised.</summary>
    public string StateName { get; }
}
