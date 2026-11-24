using System;
using KGV.Core.Security;
using Xunit;

namespace KGV.Tests;

/// <summary>FR2-08: eigenständige Rechte ohne Legacy-Autorisierung; keine HTTP-E2E.</summary>
public sealed class Fr208FachrechteTests
{
    private static UserContext User(PermissionFlags bits = PermissionFlags.None, params string[] rights)
        => new(Guid.NewGuid(), UserRole.User, 42, bits,
            PermissionFlags.None, PermissionFlags.None, PermissionFlags.None, rights);

    [Fact]
    public void LegacyBitsGewaehrleistenKeinDokumentenFachrecht()
    {
        var old = User(PermissionFlags.CanReadDocuments | PermissionFlags.CanManageDocuments
                       | PermissionFlags.CanArchiveDocuments | PermissionFlags.CanManageClubDocuments);
        Assert.False(PermissionChecks.CanReadDocuments(old));
        Assert.False(PermissionChecks.CanManageDocuments(old));
        Assert.False(PermissionChecks.CanArchiveDocuments(old));
        Assert.False(PermissionChecks.CanManageClubDocuments(old));
        Assert.False(PermissionChecks.CanManageDocumentTypes(old));
        Assert.False(PermissionChecks.CanManageDocumentEffects(old));
        Assert.False(PermissionChecks.CanManageTemplates(old));
    }

    [Fact]
    public void LesenOhneSchreibOderArchivRechte()
    {
        var reader = User(PermissionFlags.None, FachrechteV2.DokumentLesen);
        Assert.True(PermissionChecks.CanReadDocuments(reader));
        Assert.False(PermissionChecks.CanManageDocuments(reader));
        Assert.False(PermissionChecks.CanArchiveDocuments(reader));
    }

    [Fact]
    public void VerwaltenImpliziertKeinLesenOderArchivieren()
    {
        var manager = User(PermissionFlags.None, FachrechteV2.DokumentVerwalten);
        Assert.True(PermissionChecks.CanManageDocuments(manager));
        Assert.False(PermissionChecks.CanReadDocuments(manager));
        Assert.False(PermissionChecks.CanArchiveDocuments(manager));
    }

    [Fact]
    public void ArchivierenIstEigenstaendig()
    {
        var archiver = User(PermissionFlags.None, FachrechteV2.DokumentArchivieren);
        Assert.True(PermissionChecks.CanArchiveDocuments(archiver));
        Assert.False(PermissionChecks.CanManageDocuments(archiver));
        Assert.False(PermissionChecks.CanReadDocuments(archiver));
    }

    [Fact]
    public void TypWirkungVorlageUndVereinsunterlagenSindGetrennt()
    {
        var type = User(PermissionFlags.None, FachrechteV2.DokumenttypVerwalten);
        var effect = User(PermissionFlags.None, FachrechteV2.DokumentwirkungVerwalten);
        var template = User(PermissionFlags.None, FachrechteV2.VorlageVerwalten);
        var club = User(PermissionFlags.None, "vereinsunterlagen.verwalten");
        Assert.True(PermissionChecks.CanManageDocumentTypes(type));
        Assert.False(PermissionChecks.CanManageDocumentEffects(type));
        Assert.False(PermissionChecks.CanManageDocumentTypes(effect));
        Assert.True(PermissionChecks.CanManageDocumentEffects(effect));
        Assert.False(PermissionChecks.CanManageTemplates(effect));
        Assert.True(PermissionChecks.CanManageTemplates(template));
        Assert.False(PermissionChecks.CanManageClubDocuments(template));
        Assert.True(PermissionChecks.CanManageClubDocuments(club));
    }

    [Fact]
    public void AdminLeseprivilegOhneImpliziteSchreibberechtigung()
    {
        var adminReader = User(PermissionFlags.None, "vereinsdaten.alle_lesen");
        Assert.True(PermissionChecks.CanReadDocuments(adminReader));
        Assert.False(PermissionChecks.CanManageDocuments(adminReader));
        Assert.False(PermissionChecks.CanArchiveDocuments(adminReader));
        Assert.False(PermissionChecks.CanManageTemplates(adminReader));
    }

    [Fact]
    public void NullContextSchliesstJedeRechtspruefung()
    {
        Assert.False(PermissionChecks.CanReadDocuments(null));
        Assert.False(PermissionChecks.CanManageDocuments(null));
        Assert.False(PermissionChecks.CanArchiveDocuments(null));
        Assert.False(PermissionChecks.CanManageDocumentTypes(null));
        Assert.False(PermissionChecks.CanManageDocumentEffects(null));
        Assert.False(PermissionChecks.CanManageTemplates(null));
    }
}
