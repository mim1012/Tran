using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MahApps.Metro.Controls;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop.Views;

/// <summary>
/// 메인 작업 화면 (반응형 사이드바)
/// 창 너비 1000px 미만: 사이드바 아이콘만 표시 (52px)
/// 창 너비 1000px 이상: 사이드바 전체 표시 (200px)
/// </summary>
public partial class MainWorkspaceWindow : MetroWindow
{
    private bool _userOverrideSidebar;

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

        SizeChanged += OnWindowSizeChanged;
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainWorkspaceViewModel vm)
        {
            bool shouldCollapse = ActualWidth < 1000;

            if (!_userOverrideSidebar)
            {
                vm.IsSidebarCollapsed = shouldCollapse;
            }
            else
            {
                // 창이 임계값을 넘으면 수동 오버라이드 해제
                bool crossed = (e.PreviousSize.Width >= 1000) != (e.NewSize.Width >= 1000);
                if (crossed)
                {
                    _userOverrideSidebar = false;
                    vm.IsSidebarCollapsed = shouldCollapse;
                }
            }
        }
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWorkspaceViewModel vm)
        {
            vm.IsSidebarCollapsed = !vm.IsSidebarCollapsed;
            _userOverrideSidebar = true;
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

    private void CompanyTab_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is CompanyWorkspace workspace)
        {
            var vm = (MainWorkspaceViewModel)DataContext;
            vm.ActiveWorkspace = workspace;
        }
    }

    private void ProductManagementTab_Click(object sender, MouseButtonEventArgs e)
    {
        var vm = (MainWorkspaceViewModel)DataContext;
        vm.IsProductManagementMode = true;
    }

    /// <summary>
    /// 사이드바 항목 클릭 핸들러 - Tag 값으로 탭 인덱스 제어
    /// </summary>
    private void SidebarItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr && int.TryParse(tagStr, out int tabIndex))
        {
            var vm = (MainWorkspaceViewModel)DataContext;
            if (tabIndex <= 5)
            {
                vm.SelectedTabIndex = tabIndex;
            }
        }
    }
}
