using System.Windows;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 메인 작업 화면
/// 상단: 품목관리 고정 탭 + 거래처 탭 바
/// 콘텐츠: 거래처 워크스페이스(발주/견적/구매/판매/재고/통계) 또는 글로벌 품목관리
/// </summary>
public partial class MainWorkspaceWindow : Window
{
    public MainWorkspaceWindow(object? selectedCompany = null, bool productManagementMode = false)
    {
        InitializeComponent();

        var viewModel = new MainWorkspaceViewModel();
        viewModel.OnChangeCompanyRequested = () =>
        {
            var selectionWindow = new CompanySelectionWindow();
            selectionWindow.Show();
            this.Close();
        };
        viewModel.OnAddCompanyRequested = () => OpenCompanySelector();
        DataContext = viewModel;

        if (selectedCompany is Company company)
        {
            _ = viewModel.AddCompany(company);
        }

        if (productManagementMode)
        {
            viewModel.IsProductManagementMode = true;
        }
    }

    private void OpenCompanySelector()
    {
        var dialog = new CompanySelectionWindow(dialogMode: true);
        dialog.Owner = this;
        dialog.OnCompanySelected = company =>
        {
            var vm = (MainWorkspaceViewModel)DataContext;
            _ = vm.AddCompany(company);
        };
        dialog.Show();
    }

    private void OpenDataImport_Click(object sender, RoutedEventArgs e)
    {
        var importWindow = new DataImportWindow
        {
            Owner = this
        };
        importWindow.ShowDialog();
    }

    /// <summary>
    /// 거래처 추가 버튼 클릭 핸들러
    /// </summary>
    private void AddCompany_Click(object sender, RoutedEventArgs e)
    {
        OpenCompanySelector();
    }

    /// <summary>
    /// 거래처 탭 클릭 핸들러
    /// </summary>
    private void CompanyTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is System.Windows.Controls.Border border && border.DataContext is CompanyWorkspace workspace)
        {
            var vm = (MainWorkspaceViewModel)DataContext;
            vm.ActiveWorkspace = workspace;
        }
    }

    /// <summary>
    /// 품목관리 탭 클릭 핸들러
    /// </summary>
    private void ProductManagementTab_Click(object sender, MouseButtonEventArgs e)
    {
        var vm = (MainWorkspaceViewModel)DataContext;
        vm.IsProductManagementMode = true;
    }
}
