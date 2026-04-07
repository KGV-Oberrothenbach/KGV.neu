namespace KGV.Maui;

public interface IAppShellInitializer
{
    void BuildMenu();
    void SetPreferredStartupRoute(string? route);
}
