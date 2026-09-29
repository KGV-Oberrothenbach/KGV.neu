using System.Collections.Generic;
using System.Threading.Tasks;
using KGV.Core.Models;

namespace KGV.Core.Interfaces;

/// <summary>
/// Gemeinsamer Fachvertrag für die Abschlussverwaltung. Die konkrete Berechnung
/// wird serverseitig/RPC-gestützt implementiert, damit MAUI und spätere Clients
/// nicht unterschiedliche Rechenwege erhalten.
/// </summary>
public interface IJahresabschlussService
{
    Task<JahresabschlussRecord?> GetBySaisonAsync(int saisonId);
    Task<IReadOnlyList<JahresabschlussRechnungRecord>> GetRechnungenAsync(int saisonId);
    Task<JahresabschlussPruefung> PruefeAsync(SaisonRecord saison);
    Task<JahresabschlussBerechnungErgebnis> BerechneAsync(SaisonRecord saison);
    Task<JahresabschlussBerechnungErgebnis> AbschliessenAsync(SaisonRecord saison, long abgeschlossenVon);
}
