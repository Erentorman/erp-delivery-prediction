namespace App.Integration.Models;

public sealed record WorkCenterReadModel(
    string WorkCenterRef,
    string Name,
    int MachineCount,
    string? DefaultShiftRef);
