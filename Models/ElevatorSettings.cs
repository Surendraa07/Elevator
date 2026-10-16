namespace ElevatorSystem.Models;

/// <summary>
/// Building and timing constants shared by the simulation, the dispatcher's estimates and the UI.
/// </summary>
public static class ElevatorSettings
{
    /// <summary>Floors are numbered 0 .. FloorCount - 1.</summary>
    public const int FloorCount = 2;

    /// <summary>Animation timer interval (System.Windows.Forms.Timer).</summary>
    public const int TickMs = 30;

    /// <summary>Time for the car to travel one floor.</summary>
    public const int TravelMsPerFloor = 3000;

    /// <summary>Time for the doors to fully open, or fully close.</summary>
    public const int DoorMoveMs = 1200;

    /// <summary>How long the doors stay fully open before closing ("about 2 seconds").</summary>
    public const int DoorDwellMs = 2000;
}
