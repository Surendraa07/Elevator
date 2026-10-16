namespace ElevatorSystem.Models;

/// <summary>One row of the operation history, as it is written to the ElevatorLog table.</summary>
public sealed record LogEntry(DateTime EventTime, int ElevatorId, int Floor, string Status);
