using KGV.Maui.Pages;

namespace KGV.Maui;

internal static class ShellRouteRegistrar
{
    private static bool _routesRegistered;

    public static void RegisterCommonRoutes()
    {
        if (_routesRegistered)
            return;

        Routing.RegisterRoute(nameof(MeineDatenPage), typeof(MeineDatenPage));
        Routing.RegisterRoute(nameof(DokumentePage), typeof(DokumentePage));
        Routing.RegisterRoute(nameof(UserManagementPage), typeof(UserManagementPage));
        Routing.RegisterRoute(nameof(HomeSectionDetailPage), typeof(HomeSectionDetailPage));
        Routing.RegisterRoute(nameof(HomeManagementPage), typeof(HomeManagementPage));
        Routing.RegisterRoute(nameof(ExportPage), typeof(ExportPage));
        Routing.RegisterRoute(nameof(AblesungErfassenPage), typeof(AblesungErfassenPage));
        Routing.RegisterRoute(nameof(ZaehlerwechselPage), typeof(ZaehlerwechselPage));
        Routing.RegisterRoute(nameof(RfidEinrichtenPage), typeof(RfidEinrichtenPage));
        Routing.RegisterRoute(nameof(FaelligeZaehlerPage), typeof(FaelligeZaehlerPage));
        Routing.RegisterRoute(nameof(FotoUploadTestPage), typeof(FotoUploadTestPage));

        _routesRegistered = true;
    }
}
