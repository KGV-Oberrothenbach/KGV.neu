using KGV.Core.Security;
using System.Collections.Generic;

namespace KGV.Core.Models
{
    public enum HomeQuickLinkKey
    {
        MemberSearch,
        PlotManagement,
        MyProfile
    }

    public sealed class HomeQuickLinkItem
    {
        public HomeQuickLinkKey Key { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
    }

    public sealed class HomeOperationalItem
    {
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public bool IsWarning { get; init; }
    }

    public sealed class HomeOverviewDTO
    {
        public string Description { get; init; } = string.Empty;
        public string QuickLinksTitle { get; init; } = "Schnellzugriffe";
        public string QuickLinksEmptyText { get; init; } = "Für diesen Benutzerkontext sind aktuell keine gemeinsamen Schnellzugriffe verfügbar.";
        public string OperationalTitle { get; init; } = "Operative Hinweise";
        public string OperationalEmptyText { get; init; } = "Aktuell sind keine zusätzlichen belastbaren Home-Hinweise vorhanden.";
        public string AnnouncementTitle { get; init; } = "Bekanntmachungen";
        public string AnnouncementHintText { get; init; } = "Bitte eine Bekanntmachung aus der Liste auswählen.";
        public string AnnouncementEmptyText { get; init; } = "Für Home ist aktuell kein belastbarer Bekanntmachungs-Pfad angebunden.";
        public List<HomeQuickLinkItem> QuickLinks { get; init; } = new();
        public List<HomeOperationalItem> OperationalItems { get; init; } = new();
        public List<HomeAnnouncementItem> Announcements { get; init; } = new();
    }

    public static class HomeOverviewFactory
    {
        public static HomeOverviewDTO Build(UserRole role)
        {
            return role switch
            {
                UserRole.Admin or UserRole.Vorstand => new HomeOverviewDTO
                {
                    Description = "Bündelt die aktuell belastbaren operativen Schnellzugriffe für Suche und Parzellenverwaltung auf einem gemeinsamen Kernstand.",
                    QuickLinks = new List<HomeQuickLinkItem>
                    {
                        new()
                        {
                            Key = HomeQuickLinkKey.MemberSearch,
                            Title = "Mitgliedersuche",
                            Description = "Mitglieder suchen und direkt in die vorhandenen Stammdatenpfade springen."
                        },
                        new()
                        {
                            Key = HomeQuickLinkKey.PlotManagement,
                            Title = "Parzellenverwaltung",
                            Description = "Parzellen, Belegung sowie Strom-, Wasser- und Dokumentkontext zentral öffnen."
                        }
                    }
                },
                _ => new HomeOverviewDTO
                {
                    Description = "Zeigt den gemeinsamen Kernzugriff für eigene Stammdaten auf Desktop und Mobil, ohne zusätzliche Dashboard-Schattenlogik.",
                    QuickLinks = new List<HomeQuickLinkItem>
                    {
                        new()
                        {
                            Key = HomeQuickLinkKey.MyProfile,
                            Title = "Meine Stammdaten",
                            Description = "Eigene Kontaktdaten und die vorhandenen persönlichen Verwaltungswege öffnen."
                        }
                    }
                }
            };
        }
    }
}
