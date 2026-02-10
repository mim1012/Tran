using System.Windows;
using Tran.Core.Services;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 거래처 선택 화면
/// 작업할 거래처를 선택하면 MainWorkspaceWindow로 이동
/// </summary>
public partial class CompanySelectionWindow : Window
{
    public CompanySelectionWindow()
    {
        InitializeComponent();

        var viewModel = new CompanySelectionViewModel();
        viewModel.OnCompanySelected = company =>
        {
            // 선택된 거래처로 UserContext 설정
            UserContext.SetUser(company.CompanyId, company.CompanyName, company.CompanyId);

            var workspaceWindow = new MainWorkspaceWindow(company);
            workspaceWindow.Show();
            this.Close();
        };
        DataContext = viewModel;
    }
}
