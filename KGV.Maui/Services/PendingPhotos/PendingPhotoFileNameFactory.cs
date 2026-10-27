using KGV.Core.Utilities;

namespace KGV.Maui.Services.PendingPhotos;

public static class PendingPhotoFileNameFactory
{
    public static string Create(string operationType, string parzelle, string medium, DateTimeOffset? now = null)
        => SichtbarerDateiname.BuildFoto(operationType, parzelle, medium, now ?? DateTimeOffset.Now);
}
