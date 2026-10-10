using System;

namespace KGV.Core.Models;

public sealed class WorkAssignmentManagementParticipantItem
{
    public int RegistrationId { get; init; }
    public int MitgliedId { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime AngemeldetAm { get; init; }
}
