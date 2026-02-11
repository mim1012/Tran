using System.Windows;

namespace Tran.Desktop.Views;

/// <summary>
/// 재고 조정 다이얼로그
/// 품목의 재고를 수동으로 증가/감소 조정
/// </summary>
public partial class InventoryAdjustmentDialog : Window
{
    public decimal AdjustmentQuantity { get; private set; }
    public string Reason { get; private set; } = string.Empty;

    public InventoryAdjustmentDialog(string productName, decimal currentStock)
    {
        InitializeComponent();
        ProductNameText.Text = productName;
        CurrentStockText.Text = $"{currentStock:N0}";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(AdjustmentQtyBox.Text, out var qty) || qty <= 0)
        {
            MessageBox.Show("유효한 수량을 입력해 주세요.", "입력 오류",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(ReasonBox.Text))
        {
            MessageBox.Show("조정 사유를 입력해 주세요.", "입력 오류",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        // 감소 시 음수로 변환
        AdjustmentQuantity = DecreaseRadio.IsChecked == true ? -qty : qty;
        Reason = ReasonBox.Text.Trim();

        DialogResult = true;
        Close();
    }
}
