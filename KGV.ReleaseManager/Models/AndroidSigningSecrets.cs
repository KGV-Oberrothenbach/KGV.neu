namespace KGV.ReleaseManager.Models;

public sealed class AndroidSigningSecrets
{
    public string StorePassword { get; set; } = string.Empty;
    public string KeyPassword { get; set; } = string.Empty;
}
