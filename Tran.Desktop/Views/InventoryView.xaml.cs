using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 재고 관리 화면
/// 재고 현황 테이블과 입출고 이력/요약 탭을 표시
/// </summary>
public partial class InventoryView : UserControl
{
    public InventoryView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F5)
        {
            OpenAdjustmentDialog();
            e.Handled = true;
        }
    }

    private void AdjustButton_Click(object sender, RoutedEventArgs e)
    {
        OpenAdjustmentDialog();
    }

    private async void OpenAdjustmentDialog()
    {
        var vm = DataContext as InventoryViewModel;
        if (vm == null) return;

        if (vm.SelectedProduct == null)
        {
            vm.StatusMessage = "품목을 선택해 주세요.";
            return;
        }

        var selected = vm.SelectedProduct;
        var dialog = new InventoryAdjustmentDialog(
            selected.ProductName,
            selected.ConfirmedQuantity)
        {
            Owner = Window.GetWindow(this)
        };

        if (dialog.ShowDialog() == true)
        {
            await vm.ExecuteAdjustAsync(
                selected.ProductId,
                dialog.AdjustmentQuantity,
                dialog.Reason);
        }
    }
}
