namespace ElevatorSystem.Models;

/// <summary>Kinds of events an elevator publishes to its observers.</summary>
public enum ElevatorEventType
{
    HallRequest,
    CarRequest,
    MoveStarted,
    FloorPassed,
    Arrived,
    DoorsOpening,
    DoorsOpened,
    DoorsClosing,
    DoorsClosed,
    RequestServed,
    Idle,
    StateChanged
}
