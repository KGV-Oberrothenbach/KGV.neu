using System.Text.Json;
using KGV.Core.Interfaces;
using KGV.Maui.Models;
using KGV.Maui.State;
using Microsoft.Maui.Storage;

namespace KGV.Maui.Services.Documents;

public sealed class HistoricalDocumentRegistrationQueue
{
    private readonly object _sync = new();
    private readonly IVereinskontext _vereinskontext;
    private readonly UserContextState _userContext;
    private readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "pending", "historical-document-registrations.json");

    public HistoricalDocumentRegistrationQueue(IVereinskontext vereinskontext, UserContextState userContext)
    {
        _vereinskontext = vereinskontext;
        _userContext = userContext;
    }

    public IReadOnlyList<HistoricalDocumentRegistrationPending> GetForMember(int mitgliedId)
    {
        lock (_sync)
        {
            var scope = Scope();
            return Load().Where(x => x.VereinId == scope.VereinId && x.OwnerUserId == scope.UserId && x.MitgliedId == mitgliedId).ToList();
        }
    }

    public void Save(HistoricalDocumentRegistrationPending item)
    {
        lock (_sync)
        {
            var items = Load();
            var index = items.FindIndex(x => x.Id == item.Id);
            if (index < 0) items.Add(item); else items[index] = item;
            Persist(items);
        }
    }

    public void Remove(Guid id)
    {
        lock (_sync)
        {
            var items = Load();
            if (items.RemoveAll(x => x.Id == id) > 0) Persist(items);
        }
    }

    public HistoricalDocumentRegistrationPending Create(long documentId, int memberId, long typeId, DateTime date, bool signed, long? leaseId, long? assignmentId)
    {
        var scope = Scope();
        return new HistoricalDocumentRegistrationPending { VereinId = scope.VereinId, OwnerUserId = scope.UserId, DokumentId = documentId, MitgliedId = memberId, DokumenttypId = typeId, Dokumentdatum = date.Date, Unterzeichnet = signed, PachtverhaeltnisId = leaseId, WartungsvertragZuordnungId = assignmentId };
    }

    private (Guid VereinId, Guid UserId) Scope()
        => (_vereinskontext.Aktuell?.VereinId ?? throw new InvalidOperationException("Altimport benötigt einen ausgewählten Verein."), _userContext.CurrentUserId ?? throw new InvalidOperationException("Altimport benötigt einen angemeldeten Benutzer."));

    private List<HistoricalDocumentRegistrationPending> Load()
    {
        try { return File.Exists(_path) ? JsonSerializer.Deserialize<List<HistoricalDocumentRegistrationPending>>(File.ReadAllText(_path)) ?? [] : []; }
        catch { return []; }
    }

    private void Persist(List<HistoricalDocumentRegistrationPending> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(items));
    }
}
