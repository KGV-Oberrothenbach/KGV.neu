using KGV.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class TermineVerwaltungViewModel : HomeVerwaltungViewModelBase
    {
        public TermineVerwaltungViewModel(ISupabaseService supabaseService)
            : base(supabaseService)
        {
        }

        public override string Title => "Termine bearbeiten";
        public override string EmptyText => "Aktuell wurden über den bestätigten Startseiten-Lesepfad keine Termine gefunden.";
        public override string ReadPathText => "Lesepfad: v_startseite_termine";
        public override string WritePathText => "Schreibpfad ist im aktuellen Repo noch nicht belastbar verifiziert. Deshalb wird in diesem Block bewusst kein Formular mit geratenen Feldern angezeigt.";
        public override string NewCaption => "Neuer Termin";

        protected override async Task<IReadOnlyList<HomeVerwaltungListItem>> LoadEntriesCoreAsync()
        {
            var items = await SupabaseService.GetStartseiteTermineAsync();
            var result = new List<HomeVerwaltungListItem>();
            foreach (var item in items)
            {
                result.Add(new HomeVerwaltungListItem
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    Content = item.Details
                });
            }

            return result;
        }
    }
}
