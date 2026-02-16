using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Tran.Core.Services;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

public partial class DataImportWindow : MetroWindow
{
    public DataImportWindow()
    {
        InitializeComponent();
    }

    private void ImportType_Checked(object sender, RoutedEventArgs e)
    {
        if (DataContext is not DataImportViewModel vm) return;
        if (sender is not RadioButton rb) return;

        var tag = rb.Tag?.ToString();
        if (Enum.TryParse<ImportType>(tag, out var importType))
        {
            vm.SelectedImportType = importType;
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
