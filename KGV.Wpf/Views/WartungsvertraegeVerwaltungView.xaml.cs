using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using KGV.ViewModels;

namespace KGV.Views
{
    public partial class WartungsvertraegeVerwaltungView : UserControl
    {
        public WartungsvertraegeVerwaltungView()
        {
            InitializeComponent();
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid dataGrid || DataContext is not WartungsvertraegeVerwaltungViewModel viewModel)
                return;

            var dependencyObject = e.OriginalSource as DependencyObject;
            while (dependencyObject != null && dependencyObject is not DataGridRow)
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);

            if (dependencyObject is not DataGridRow || dataGrid.SelectedItem == null)
                return;

            if (viewModel.OpenCommand.CanExecute(null))
                viewModel.OpenCommand.Execute(null);
        }
    }
}
