using Microsoft.Maui.Storage;

namespace KGV.Maui.Services;

public interface IClientInstanceIdProvider
{
    Guid GetClientInstanceId();
}

public sealed class ClientInstanceIdProvider : IClientInstanceIdProvider
{
    private const string PreferenceKey = "kgv.client-instance.v1";

    public Guid GetClientInstanceId()
    {
        var stored = Preferences.Default.Get(PreferenceKey, string.Empty);
        if (Guid.TryParse(stored, out var clientInstanceId) && clientInstanceId != Guid.Empty)
            return clientInstanceId;

        clientInstanceId = Guid.NewGuid();
        Preferences.Default.Set(PreferenceKey, clientInstanceId.ToString());
        return clientInstanceId;
    }
}
