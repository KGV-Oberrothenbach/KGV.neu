using KGV.Core.Security;
using System;

namespace KGV.Core.Models
{
    public sealed class MemberUserLinkStatusDto
    {
        public int MitgliedId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Role { get; set; } = UserRoles.User;
        public string Email { get; set; } = string.Empty;
        public Guid? MitgliedAuthUserId { get; set; }
        public Guid? AppUserUserId { get; set; }
        public int? AppUserMitgliedId { get; set; }
        public MemberUserLinkStatus Status { get; set; }
        public bool CanInvite { get; set; }
        public bool CanRemove { get; set; }
        public bool IsConsistent { get; set; }
        public string? WarningText { get; set; }
        public string StatusText => Status switch
        {
            MemberUserLinkStatus.None => "Kein Nutzer verknüpft",
            MemberUserLinkStatus.Consistent => "Vollständig verknüpft",
            MemberUserLinkStatus.MissingMemberAuthLink => "App-User vorhanden, Mitglied-Link fehlt",
            MemberUserLinkStatus.MissingAppUser => "Mitglied-Link vorhanden, App-User fehlt",
            MemberUserLinkStatus.Conflict => "Konflikt",
            _ => Status.ToString()
        };
    }
}
