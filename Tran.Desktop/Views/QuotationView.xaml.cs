using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 견적서 관리 화면
/// 견적 작성 폼 + 단가 변경 미리보기 + 견적 목록/단가 이력 탭
/// </summary>
public partial class QuotationView : UserControl
{
    public QuotationView()
    {
        InitializeComponent();
        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F7)
        {
            OpenProductSelectionPopup();
            e.Handled = true;
        }
    }

    private void AddProduct_Click(object sender, RoutedEventArgs e)
    {
        OpenProductSelectionPopup();
    }

    private async void OpenProductSelectionPopup()
    {
        var vm = DataContext as QuotationViewModel;
        if (vm == null) return;

        var popup = new ProductSelectionPopup(Window.GetWindow(this));
        var popupVm = popup.DataContext as ProductSelectionPopupViewModel;
        if (popupVm == null) return;

        await popupVm.LoadProductsAsync(vm.CompanyId);

        popupVm.OnConfirmed = selectedProducts =>
        {
            foreach (var row in selectedProducts)
            {
                var product = new Product
                {
                    ProductId = row.ProductId,
                    ProductName = row.ProductName,
                    DefaultPrice = row.UnitPrice
                };
                vm.AddProductCommand.Execute(product);
            }
            popup.DialogResult = true;
            popup.Close();
        };

        popupVm.OnCancelled = () =>
        {
            popup.DialogResult = false;
            popup.Close();
        };

        popup.ShowDialog();
    }
}
