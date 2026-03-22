using KGV.Core.Interfaces;
using KGV.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KGV.ViewModels
{
    public sealed class ArbeitseinsaetzeVerwaltungViewModel : HomeVerwaltungViewModelBase
    {
        public ArbeitseinsaetzeVerwaltungViewModel(ISupabaseService supabaseService)
            : base(supabaseService)
        {
        }

        public override string Title => "Arbeitseinsätze bearbeiten";
        public override string EmptyText => "Aktuell wurden über den bestätigten Startseiten-Lesepfad keine Arbeitseinsätze gefunden.";
        public override string ReadPathText => "Lesepfad: v_startseite_arbeitseinsatz";
        public override string WritePathText => "Schreibpfad ist im aktuellen Repo noch nicht belastbar verifiziert. Deshalb wird in diesem Block bewusst kein Formular mit geratenen Feldern angezeigt.";
        public override string NewCaption => "Neuer Arbeitseinsatz";

        protected override async Task<IReadOnlyList<HomeVerwaltungListItem>> LoadEntriesCoreAsync()
        {
            var items = await SupabaseService.GetStartseiteArbeitseinsaetzeAsync();
            var result = new List<HomeVerwaltungListItem>();
            foreach (var item in items)
            {
                result.Add(new HomeVerwaltungListItem
                {
                    Title = item.Title,
                    Subtitle = item.Subtitle,
                    Content = item.Details,
                    AdditionalInfo = item.RegistrationInfo
                });
            }

            return result;
        }
    }
}
