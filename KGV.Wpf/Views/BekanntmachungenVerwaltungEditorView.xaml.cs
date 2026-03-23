using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class BekanntmachungenVerwaltungEditorView : UserControl
    {
        private INotifyPropertyChanged? _currentViewModel;

        public BekanntmachungenVerwaltungEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += (_, _) => RefreshPreview();
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _currentViewModel = DataContext as INotifyPropertyChanged;
            if (_currentViewModel != null)
                _currentViewModel.PropertyChanged += OnViewModelPropertyChanged;

            RefreshPreview();
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (DataContext is not BekanntmachungenVerwaltungViewModel vm)
                return;

            if (e.PropertyName == nameof(BekanntmachungenVerwaltungViewModel.FocusRequestToken))
                Dispatcher.BeginInvoke(() => FocusRequestedControl(vm.FocusTarget), DispatcherPriority.Input);

            if (e.PropertyName == nameof(BekanntmachungenVerwaltungViewModel.InhaltHtml)
                || e.PropertyName == nameof(BekanntmachungenVerwaltungViewModel.IsEditorOpen)
                || e.PropertyName == nameof(BekanntmachungenVerwaltungViewModel.EditorCaption))
            {
                Dispatcher.BeginInvoke(RefreshPreview, DispatcherPriority.Background);
            }
        }

        private void OnEntriesListBoxMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not ListBox { SelectedItem: not null } listBox)
                return;

            if (DataContext is not BekanntmachungenVerwaltungViewModel vm)
                return;

            if (!vm.OeffnenCommand.CanExecute(listBox.SelectedItem))
                return;

            vm.OeffnenCommand.Execute(listBox.SelectedItem);
            e.Handled = true;
        }

        private void FocusRequestedControl(string target)
        {
            switch (target)
            {
                case BekanntmachungenVerwaltungViewModel.FocusTitel:
                    TitelTextBox.Focus();
                    TitelTextBox.SelectAll();
                    break;
                case BekanntmachungenVerwaltungViewModel.FocusInhaltHtml:
                    InhaltHtmlTextBox.Focus();
                    InhaltHtmlTextBox.SelectAll();
                    break;
                case BekanntmachungenVerwaltungViewModel.FocusSichtbarAb:
                    SichtbarAbDatePicker.Focus();
                    break;
                case BekanntmachungenVerwaltungViewModel.FocusSichtbarBis:
                    SichtbarBisDatePicker.Focus();
                    break;
                case BekanntmachungenVerwaltungViewModel.FocusSortOrder:
                    SortOrderTextBox.Focus();
                    SortOrderTextBox.SelectAll();
                    break;
            }
        }

        private void InsertHtmlSnippet_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: string key })
                return;

            var selectedText = InhaltHtmlTextBox.SelectedText;
            string snippet = key switch
            {
                "paragraph" => $"<p>{GetSelectionOrDefault(selectedText, "Text")}</p>",
                "heading" => $"<h3>{GetSelectionOrDefault(selectedText, "Überschrift")}</h3>",
                "strong" => $"<strong>{GetSelectionOrDefault(selectedText, "Betonung")}</strong>",
                "link" => selectedText.Contains("href=") ? selectedText : $"<a href=\"https://\">{GetSelectionOrDefault(selectedText, "Linktext")}</a>",
                "list" => "<ul>\r\n  <li>Punkt 1</li>\r\n  <li>Punkt 2</li>\r\n</ul>",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(snippet))
                return;

            var replacement = snippet;
            var selectionStart = InhaltHtmlTextBox.SelectionStart;
            InhaltHtmlTextBox.SelectedText = replacement;
            InhaltHtmlTextBox.Focus();
            InhaltHtmlTextBox.SelectionStart = selectionStart + replacement.Length;
            InhaltHtmlTextBox.SelectionLength = 0;
        }

        private static string GetSelectionOrDefault(string selectedText, string fallback)
        {
            return string.IsNullOrWhiteSpace(selectedText) ? fallback : selectedText;
        }

        private void RefreshPreview()
        {
            if (DataContext is not BekanntmachungenVerwaltungViewModel vm || !vm.IsEditorOpen)
            {
                HtmlPreviewBrowser.NavigateToString("<html><body style='font-family:Segoe UI;padding:12px;color:#666;'>Keine Bekanntmachung geöffnet.</body></html>");
                return;
            }

            var html = string.IsNullOrWhiteSpace(vm.InhaltHtml)
                ? "<p style='color:#666;'>Noch kein HTML-Inhalt vorhanden.</p>"
                : vm.InhaltHtml;

            var document = $"<html><head><meta charset='utf-8'><style>body{{font-family:'Segoe UI';padding:16px;}} table{{border-collapse:collapse;}} td,th{{border:1px solid #ccc;padding:4px;}}</style></head><body>{html}</body></html>";
            HtmlPreviewBrowser.NavigateToString(document);
        }
    }
}
