namespace ElevatorSystem.Models;

/// <summary>Raised by the dispatcher when a floor's request button should light up or reset.</summary>
public sealed class HallCallEventArgs : EventArgs
{
    public HallCallEventArgs(int floor, bool isActive, Elevator? assignedElevator)
    {
        Floor = floor;
        IsActive = isActive;
        AssignedElevator = assignedElevator;
    }

    public int Floor { get; }

    /// <summary>True when the call is waiting for service, false once it has been served.</summary>
    public bool IsActive { get; }

    public Elevator? AssignedElevator { get; }
}
