using KGV.Core.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class BekanntmachungenVerwaltungViewModel : HomeVerwaltungViewModelBase
    {
        public BekanntmachungenVerwaltungViewModel(ISupabaseService supabaseService)
            : base(supabaseService)
        {
        }

        public override string Title => "Bekanntmachungen bearbeiten";
        public override string EmptyText => "Aktuell wurden über den bestätigten Startseiten-Lesepfad keine Bekanntmachungen gefunden.";
        public override string ReadPathText => "Lesepfad: v_startseite_bekanntmachungen";
        public override string WritePathText => "Schreibpfad ist im aktuellen Repo noch nicht belastbar verifiziert. Deshalb wird in diesem Block bewusst kein Formular mit geratenen Feldern angezeigt.";
        public override string NewCaption => "Neue Bekanntmachung";

        protected override async Task<IReadOnlyList<HomeVerwaltungListItem>> LoadEntriesCoreAsync()
        {
            var items = await SupabaseService.GetStartseiteBekanntmachungenAsync();
            var result = new List<HomeVerwaltungListItem>();
            foreach (var item in items)
            {
                result.Add(new HomeVerwaltungListItem
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    Content = item.Content
                });
            }

            return result;
        }
    }
}
