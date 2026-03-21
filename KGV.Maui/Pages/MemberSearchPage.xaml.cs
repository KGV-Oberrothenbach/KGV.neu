using KGV.Maui.ViewModels;
using System.Linq;

namespace KGV.Maui.Pages;

public partial class MemberSearchPage : ContentPage
{
    private readonly MemberSearchViewModel _vm;

    public MemberSearchPage(MemberSearchViewModel vm)
    {
        InitializeComponent();

        _vm = vm;
        BindingContext = _vm;

        Appearing += MemberSearchPage_Appearing;
    }

    private async void MemberSearchPage_Appearing(object? sender, EventArgs e)
    {
        Appearing -= MemberSearchPage_Appearing;
        await _vm.InitializeAsync();
    }

    private async void ResultsCollectionView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        var item = e.CurrentSelection?.FirstOrDefault() as MemberSearchResultItem;

        if (sender is CollectionView cv)
            cv.SelectedItem = null;

        var member = await _vm.SelectResultAsync(item);
        if (member == null)
            return;

        await DisplayAlert("Mitglied gewählt", $"{member.DisplayName} (Id: {member.MemberId})", "OK");
    }
}
