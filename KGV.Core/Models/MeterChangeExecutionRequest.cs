namespace KGV.Core.Models;

/// <summary>All values for the single server-side technical meter change.</summary>
public sealed record MeterChangeExecutionRequest(
    long OldMeterId, long ParcelId, string Medium, DateTime RemovalDate, decimal FinalValue,
    string? RemovalPhotoPath, string NewMeterNumber, DateTime CalibrationDate,
    DateTime InstallationDate, decimal InitialValue, string? InstallationPhotoPath);
