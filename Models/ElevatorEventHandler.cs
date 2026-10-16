namespace ElevatorSystem.Models;

/// <summary>Delegate type for the elevator's custom events (ElevatorArrived, DoorsOpened, DoorsClosed, ...).</summary>
public delegate void ElevatorEventHandler(Elevator sender, ElevatorEventArgs e);
