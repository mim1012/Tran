using System.Windows;
using Tran.Core.Models;
using Tran.Core.Services;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 거래처 선택 화면
/// 기본 모드: 작업할 거래처를 선택하면 MainWorkspaceWindow로 이동
/// 다이얼로그 모드: 거래처 선택 후 콜백 호출 + 자신 닫기
/// </summary>
public partial class CompanySelectionWindow : Window
{
    /// <summary>
    /// 다이얼로그 모드에서 거래처 선택 시 콜백
    /// </summary>
    public Action<Company>? OnCompanySelected { get; set; }

    private readonly bool _dialogMode;

    public CompanySelectionWindow() : this(dialogMode: false) { }

    public CompanySelectionWindow(bool dialogMode)
    {
        _dialogMode = dialogMode;
        InitializeComponent();

        var viewModel = new CompanySelectionViewModel();
        viewModel.OnCompanySelected = company =>
        {
            if (_dialogMode)
            {
                // 다이얼로그 모드: 콜백 호출 후 닫기
                OnCompanySelected?.Invoke(company);
                this.Close();
            }
            else
            {
                // 기존 모드: UserContext 설정 + 새 창 열기
                UserContext.SetUser(company.CompanyId, company.CompanyName, company.CompanyId);
                var workspaceWindow = new MainWorkspaceWindow(company);
                workspaceWindow.Show();
                this.Close();
            }
        };
        DataContext = viewModel;
    }
}
