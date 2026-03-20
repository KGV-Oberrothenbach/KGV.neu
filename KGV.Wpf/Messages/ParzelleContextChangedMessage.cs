using KGV.Core.Models;

namespace KGV.Messages
{
    public sealed record ParzelleContextChangedMessage(ParzellenBelegungDTO? Belegung);
}
