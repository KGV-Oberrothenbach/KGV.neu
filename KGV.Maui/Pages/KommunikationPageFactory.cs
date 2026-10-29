using KGV.Core.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KGV.Maui.Pages;

/// <summary>
/// Creates communication pages for a fixed functional area while preserving
/// the legacy, unscoped page resolution used by the current shell route.
/// </summary>
public sealed class KommunikationPageFactory(IServiceProvider services)
{
    private readonly IServiceProvider _services = services ?? throw new ArgumentNullException(nameof(services));

    public KommunikationPage Create() => _services.GetRequiredService<KommunikationPage>();

    public KommunikationPage Create(CommunicationArea area)
    {
        if (!Enum.IsDefined(area))
            throw new ArgumentOutOfRangeException(nameof(area), area, "Unknown communication area.");

        return ActivatorUtilities.CreateInstance<KommunikationPage>(_services, area);
    }
}
