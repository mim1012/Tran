using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tran.Data;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// 양식 관리 화면 (완전한 읽기 전용)
/// - 편집/저장 버튼 없음
/// - ICommand 없음
/// - "관찰 전용 레이어"
/// </summary>
public partial class TemplateManagementWindow : Window
{
    private readonly TemplateManagementViewModel _viewModel;

    public TemplateManagementWindow()
    {
        InitializeComponent();

        // DbContext 생성 (DI 컨테이너 없이 직접 생성)
        var optionsBuilder = new DbContextOptionsBuilder<TranDbContext>();
        optionsBuilder.UseSqlite("Data Source=tran.db");
        var dbContext = new TranDbContext(optionsBuilder.Options);

        // ViewModel 초기화
        _viewModel = new TemplateManagementViewModel(dbContext);
        DataContext = _viewModel;

        // 비동기 로드
        Loaded += async (s, e) => await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// 활성/비활성 필터 변경
    /// </summary>
    private void ActiveFilterComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_viewModel == null) return;

        // 0: 활성만, 1: 전체
        _viewModel.ShowActiveOnly = ActiveFilterComboBox.SelectedIndex == 0;
    }

    /// <summary>
    /// 새로고침 버튼
    /// </summary>
    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoadTemplatesAsync();
    }

    /// <summary>
    /// 닫기 버튼
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
