using System;

namespace KGV.Core.Models;

public static class InsertRecordMappingExtensions
{
    public static ArbeitsstundeInsertRecord ToInsertRecord(this ArbeitsstundeRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new ArbeitsstundeInsertRecord
        {
            MitgliedId = record.MitgliedId,
            SaisonId = record.SaisonId,
            Datum = record.Datum,
            Stunden = record.Stunden,
            ArtDerArbeit = record.ArtDerArbeit,
            Status = record.Status,
            Freigegeben = record.Freigegeben,
            GenehmigtAm = record.GenehmigtAm,
            GenehmigtVon = record.GenehmigtVon,
            LockedByUserId = record.LockedByUserId,
            LockedAt = record.LockedAt
        };
    }

    public static ArbeitseinsatzInsertRecord ToInsertRecord(this ArbeitseinsatzRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new ArbeitseinsatzInsertRecord
        {
            Titel = record.Titel,
            Beschreibung = record.Beschreibung,
            Datum = record.Datum,
            StartUhrzeit = record.StartUhrzeit,
            EndUhrzeit = record.EndUhrzeit,
            Treffpunkt = record.Treffpunkt,
            MaxTeilnehmer = record.MaxTeilnehmer,
            StundenWert = record.StundenWert,
            SichtbarAb = record.SichtbarAb,
            SichtbarBis = record.SichtbarBis,
            AnmeldungBis = record.AnmeldungBis,
            Aktiv = record.Aktiv,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            IsDemo = record.IsDemo
        };
    }

    public static TerminInsertRecord ToInsertRecord(this TerminRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new TerminInsertRecord
        {
            Titel = record.Titel,
            Beschreibung = record.Beschreibung,
            Datum = record.Datum,
            StartUhrzeit = record.StartUhrzeit,
            EndUhrzeit = record.EndUhrzeit,
            SichtbarAb = record.SichtbarAb,
            SichtbarBis = record.SichtbarBis,
            Aktiv = record.Aktiv,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public static BekanntmachungInsertRecord ToInsertRecord(this BekanntmachungRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new BekanntmachungInsertRecord
        {
            Titel = record.Titel,
            InhaltHtml = record.InhaltHtml,
            SichtbarAb = record.SichtbarAb,
            SichtbarBis = record.SichtbarBis,
            SortOrder = record.SortOrder,
            Aktiv = record.Aktiv,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt
        };
    }

    public static WartungsvertragInsertRecord ToInsertRecord(this WartungsvertragRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new WartungsvertragInsertRecord
        {
            Titel = record.Titel,
            Beschreibung = record.Beschreibung,
            Bereich = record.Bereich,
            MaxAktiveZuordnungen = record.MaxAktiveZuordnungen,
            BefreitVonPflichtstunden = record.BefreitVonPflichtstunden,
            Aktiv = record.Aktiv,
            Bemerkung = record.Bemerkung,
            CreatedAt = record.CreatedAt,
            UpdatedAt = record.UpdatedAt,
            IsDemo = record.IsDemo
        };
    }
}
