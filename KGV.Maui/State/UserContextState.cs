using KGV.Core.Security;
using KGV.Maui.Settings;
using KGV.Maui.Services;
using System;
using System.Collections.Generic;

namespace KGV.Maui.State;

public sealed class UserContextState : IUserContextAccessor
{
    public Guid? CurrentUserId { get; set; }
    public long? CurrentMitgliedId { get; set; }
    public long? CurrentNebenMitgliedId { get; set; }

    public AppMode? CurrentAppMode { get; set; }
    public UserContext? CurrentUserContext { get; set; }

    // AP-02.3A: nur schreibgeschuetzte Schattenrechte, kein lokaler API-Guard.
    public IReadOnlyList<FachrechtV2Record>? FachrechteV2 { get; private set; }
    public DateTimeOffset? FachrechteV2AktualisiertAm { get; private set; }

    public void SetFachrechteV2(IReadOnlyList<FachrechtV2Record> rights)
    {
        FachrechteV2 = rights;
        FachrechteV2AktualisiertAm = DateTimeOffset.UtcNow;
    }

    public void ClearFachrechteV2()
    {
        FachrechteV2 = null;
        FachrechteV2AktualisiertAm = null;
    }

    public void Clear()
    {
        CurrentUserId = null;
        CurrentMitgliedId = null;
        CurrentNebenMitgliedId = null;
        CurrentUserContext = null;
        ClearFachrechteV2();
        CurrentAppMode = null;
    }
}
