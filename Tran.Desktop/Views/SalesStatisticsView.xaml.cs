using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tran.Core.Services;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

public partial class SalesStatisticsView : UserControl
{
    public SalesStatisticsView()
    {
        InitializeComponent();
    }

    private void Category_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is CategorySalesDto category
            && DataContext is SalesStatisticsViewModel vm)
        {
            vm.SelectedCategory = category;
        }
    }
}
