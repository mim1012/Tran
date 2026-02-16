using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 거래처 선택 화면
/// 기본 모드: 작업할 거래처를 선택하면 MainWorkspaceWindow로 이동
/// 다이얼로그 모드: 거래처 선택 후 콜백 호출 + 자신 닫기
/// </summary>
public partial class CompanySelectionWindow : MetroWindow
{
    /// <summary>
    /// 다이얼로그 모드에서 거래처 선택 시 콜백
    /// </summary>
    public Action<Company>? OnCompanySelected { get; set; }

    private readonly bool _dialogMode;
    private readonly CompanySelectionViewModel _viewModel;

    public CompanySelectionWindow() : this(dialogMode: false) { }

    public CompanySelectionWindow(bool dialogMode)
    {
        _dialogMode = dialogMode;
        InitializeComponent();

        _viewModel = new CompanySelectionViewModel();
        _viewModel.OnCompanySelected = company =>
        {
            if (_dialogMode)
            {
                OnCompanySelected?.Invoke(company);
                this.Close();
            }
            else
            {
                UserContext.SetUser(company.CompanyId, company.CompanyName, company.CompanyId);
                var workspaceWindow = new MainWorkspaceWindow(company);
                workspaceWindow.Show();
                this.Close();
            }
        };

        _viewModel.OnAddNewCompanyRequested = () =>
        {
            var dialog = new CompanyAddDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _ = _viewModel.ReloadAsync();
            }
        };

        _viewModel.OnEditCompanyRequested = company =>
        {
            var dialog = new CompanyAddDialog(company) { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                _ = _viewModel.ReloadAsync();
            }
        };

        _viewModel.OnProductManagementRequested = () =>
        {
            var workspaceWindow = new MainWorkspaceWindow(productManagementMode: true);
            workspaceWindow.Show();
            this.Close();
        };

        DataContext = _viewModel;
    }

    private void CompanyGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGrid grid && grid.SelectedItem is Company company)
        {
            _viewModel.SelectCompanyCommand?.Execute(company);
        }
    }
}
