namespace App.Integration.Models;

internal sealed record WorkCenterReadModel(
    string WorkCenterReference,
    string Name,
    int MachineCount,
    string? DefaultShiftReference);
