using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 발주 화면 (UserControl)
/// 3분할 레이아웃: 상단 좌(자주 거래 품목) + 상단 우(전체 품목) / 하단(등록/임시저장/이력)
/// Blue 액센트 (#1E90FF)
/// </summary>
public partial class OrderView : UserControl
{
    private Point _dragStartPoint;

    public OrderView()
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
        var vm = DataContext as OrderViewModel;
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

    /// <summary>
    /// 전체 품목 그리드 더블클릭 시 발주 목록에 추가
    /// </summary>
    private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // DataGrid에서 더블클릭된 행이 있으면 ViewModel의 AddProduct 커맨드 실행
        if (sender is DataGrid grid && grid.SelectedItem != null)
        {
            var addCommand = (DataContext as dynamic)?.AddProductCommand as ICommand;
            if (addCommand?.CanExecute(grid.SelectedItem) == true)
            {
                addCommand.Execute(grid.SelectedItem);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 드래그앤드롭: 전체 품목 DataGrid (드래그 소스)
    // ═══════════════════════════════════════════════════════════

    private void ProductGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void ProductGrid_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var diff = _dragStartPoint - e.GetPosition(null);
        if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is DataGrid grid && grid.SelectedItem is Product product)
        {
            DragDrop.DoDragDrop(grid, product, DragDropEffects.Copy);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 드래그앤드롭: 자주 거래 품목 ListBox (드래그 소스)
    // ═══════════════════════════════════════════════════════════

    private void FrequentListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private void FrequentListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;

        var diff = _dragStartPoint - e.GetPosition(null);
        if (Math.Abs(diff.X) <= SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) <= SystemParameters.MinimumVerticalDragDistance) return;

        if (sender is ListBox listBox && listBox.SelectedItem is CompanyProduct companyProduct)
        {
            DragDrop.DoDragDrop(listBox, companyProduct, DragDropEffects.Copy);
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 드래그앤드롭: 자주 거래 품목 ListBox (드롭 대상)
    // 전체 품목에서 자주 거래 품목으로 드래그앤드롭
    // ═══════════════════════════════════════════════════════════

    private void FrequentListBox_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Product)))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void FrequentListBox_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Product)))
        {
            // 전체 품목 → 자주 거래 품목으로 추가
            var vm = DataContext as OrderViewModel;
            if (vm == null) return;

            var product = e.Data.GetData(typeof(Product)) as Product;
            if (product != null)
            {
                vm.SelectedProduct = product;
                vm.AddToFrequentCommand.Execute(null);
            }
        }
    }

    // ═══════════════════════════════════════════════════════════
    // 드래그앤드롭: 하단 발주 목록 DataGrid (드롭 대상)
    // ═══════════════════════════════════════════════════════════

    private void OrderItemsGrid_DragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Product)) || e.Data.GetDataPresent(typeof(CompanyProduct)))
        {
            e.Effects = DragDropEffects.Copy;
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OrderItemsGrid_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(Product)))
        {
            var product = e.Data.GetData(typeof(Product)) as Product;
            var addCommand = (DataContext as dynamic)?.AddProductCommand as ICommand;
            if (addCommand?.CanExecute(product) == true)
            {
                addCommand.Execute(product);
            }
        }
        else if (e.Data.GetDataPresent(typeof(CompanyProduct)))
        {
            var companyProduct = e.Data.GetData(typeof(CompanyProduct)) as CompanyProduct;
            var addCommand = (DataContext as dynamic)?.AddFrequentProductCommand as ICommand;
            if (addCommand?.CanExecute(companyProduct) == true)
            {
                addCommand.Execute(companyProduct);
            }
        }
    }
}
