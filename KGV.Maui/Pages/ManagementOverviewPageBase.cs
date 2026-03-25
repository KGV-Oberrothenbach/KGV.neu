using KGV.Core.Interfaces;

namespace KGV.Maui.Pages;

public abstract class ManagementOverviewPageBase : ContentPage
{
    private readonly List<ManagementOverviewEntry> _entries = new();
    private readonly CollectionView _entriesView;
    private readonly Label _countLabel;
    private readonly Label _statusLabel;
    private bool _isLoading;

    protected ManagementOverviewPageBase(ISupabaseService supabaseService)
    {
        SupabaseService = supabaseService;
        Title = PageTitle;

        _countLabel = new Label { FontSize = 12, TextColor = Colors.Gray };
        _statusLabel = new Label { TextColor = Colors.DarkSlateBlue, LineBreakMode = LineBreakMode.WordWrap };

        var refreshButton = new Button { Text = "Aktualisieren" };
        refreshButton.Clicked += async (_, _) => await LoadAsync();

        var newButton = new Button { Text = "Neu" };
        newButton.Clicked += async (_, _) => await OpenTechnicalContinuationAsync(entryId: null, isNew: true);

        _entriesView = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            ItemsSource = _entries,
            EmptyView = new Label
            {
                Text = EmptyText,
                TextColor = Colors.Gray
            },
            ItemTemplate = new DataTemplate(() =>
            {
                var title = new Label { FontAttributes = FontAttributes.Bold };
                title.SetBinding(Label.TextProperty, nameof(ManagementOverviewEntry.Title));

                var subtitle = new Label { FontSize = 12, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap };
                subtitle.SetBinding(Label.TextProperty, nameof(ManagementOverviewEntry.Subtitle));

                var hint = new Label { FontSize = 12, TextColor = Colors.DarkSlateBlue, Text = HintText };

                return new Border
                {
                    Stroke = Colors.LightGray,
                    Padding = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    Content = new VerticalStackLayout
                    {
                        Spacing = 4,
                        Children = { title, subtitle, hint }
                    }
                };
            })
        };
        _entriesView.SelectionChanged += async (_, e) =>
        {
            var selected = e.CurrentSelection?.FirstOrDefault() as ManagementOverviewEntry;
            if (selected == null)
                return;

            _entriesView.SelectedItem = null;
            await OpenTechnicalContinuationAsync(selected.Id, isNew: false);
        };

        Content = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Padding = 24,
                Spacing = 12,
                Children =
                {
                    new Label { Text = PageTitle, FontSize = 24, FontAttributes = FontAttributes.Bold },
                    new Label { Text = PageDescription, TextColor = Colors.Gray, LineBreakMode = LineBreakMode.WordWrap },
                    new HorizontalStackLayout
                    {
                        Spacing = 8,
                        Children = { refreshButton, newButton }
                    },
                    _countLabel,
                    _statusLabel,
                    _entriesView
                }
            }
        };
    }

    protected ISupabaseService SupabaseService { get; }
    protected abstract string PageTitle { get; }
    protected abstract string PageDescription { get; }
    protected abstract string SectionQueryValue { get; }
    protected virtual string EmptyText => "Aktuell liegen keine Datensätze vor.";
    protected virtual string HintText => "Antippen öffnet den technischen Fortsetzungspfad für diesen Bereich.";

    protected abstract Task<IReadOnlyList<ManagementOverviewEntry>> LoadEntriesCoreAsync();

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        _statusLabel.Text = string.Empty;

        try
        {
            var loadedEntries = await LoadEntriesCoreAsync();
            _entries.Clear();
            foreach (var entry in loadedEntries)
                _entries.Add(entry);

            _entriesView.ItemsSource = null;
            _entriesView.ItemsSource = _entries;
            _countLabel.Text = _entries.Count > 0
                ? $"{_entries.Count} Datensatz/Datensätze"
                : EmptyText;
        }
        catch (Exception ex)
        {
            _statusLabel.Text = ex.Message;
            _countLabel.Text = string.Empty;
        }
        finally
        {
            _isLoading = false;
        }
    }

    private Task OpenTechnicalContinuationAsync(long? entryId, bool isNew)
    {
        var route = $"{nameof(HomeManagementPage)}?section={SectionQueryValue}&lockSection=true";
        if (isNew)
            route += "&mode=new";
        else if (entryId.HasValue)
            route += $"&entryId={entryId.Value}";

        return Shell.Current.GoToAsync(route);
    }

    protected sealed record ManagementOverviewEntry(long Id, string Title, string Subtitle);
}
