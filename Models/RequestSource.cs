namespace ElevatorSystem.Models;

/// <summary>Where a floor request came from.</summary>
public enum RequestSource
{
    /// <summary>A request (call) button on a floor, outside the elevator.</summary>
    Hall,

    /// <summary>A floor button on the control panel inside the car.</summary>
    Car
}
