using System.Windows;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 메인 작업 화면
/// 상단 거래처 정보 바 + 탭 컨트롤(발주/견적/구매/판매/재고/품목)
/// </summary>
public partial class MainWorkspaceWindow : Window
{
    public MainWorkspaceWindow(object? selectedCompany = null)
    {
        InitializeComponent();

        var viewModel = new MainWorkspaceViewModel();
        viewModel.OnChangeCompanyRequested = () =>
        {
            var selectionWindow = new CompanySelectionWindow();
            selectionWindow.Show();
            this.Close();
        };
        DataContext = viewModel;

        if (selectedCompany is Company company)
        {
            _ = viewModel.SetCompany(company);
        }
    }

    /// <summary>
    /// 거래처 변경 버튼 클릭 핸들러
    /// 확인 후 CompanySelectionWindow를 열고 현재 창을 닫음
    /// </summary>
    private void ChangeCompany_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "거래처를 변경하시겠습니까?\n저장하지 않은 작업은 유실될 수 있습니다.",
            "거래처 변경",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            var selectionWindow = new CompanySelectionWindow();
            selectionWindow.Show();
            this.Close();
        }
    }
}
