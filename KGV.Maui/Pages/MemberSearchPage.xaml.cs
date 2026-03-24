using KGV.Maui.ViewModels;
using KGV.Maui.State;
using System.Linq;

namespace KGV.Maui.Pages;

public partial class MemberSearchPage : ContentPage
{
    private readonly MemberSearchViewModel _vm;
    private readonly MemberContextState _memberContextState;

    public MemberSearchPage(MemberSearchViewModel vm, MemberContextState memberContextState)
    {
        InitializeComponent();

        _vm = vm;
        _memberContextState = memberContextState;
        BindingContext = _vm;

        Appearing += MemberSearchPage_Appearing;
    }

    private async void MemberSearchPage_Appearing(object? sender, EventArgs e)
    {
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

        _memberContextState.SetSelectedMember(member);
        await Shell.Current.GoToAsync("//memberdetails");
    }
}
