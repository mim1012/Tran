using System.Windows;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// 정산 관리 화면 (읽기 전용)
/// Confirmed 상태 문서의 집계 및 조회만 수행
/// </summary>
public partial class SettlementManagementWindow : Window
{
    public SettlementManagementWindow(SettlementManagementViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

        // 초기 데이터 로드
        Loaded += async (s, e) =>
        {
            var vm = DataContext as SettlementManagementViewModel;
            if (vm != null)
            {
                // ICommand.Execute는 void를 반환하므로 async 메서드 직접 호출
                await vm.LoadSummariesAsync();
            }
        };
    }

    /// <summary>
    /// 문서 목록 더블클릭 이벤트 핸들러
    /// DocumentDetailWindow를 열어 상세 정보 표시
    /// </summary>
    private void DocumentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var vm = DataContext as SettlementManagementViewModel;
        if (vm?.SelectedDocument == null)
            return;

        try
        {
            // DocumentDetailWindow 열기
            // DocumentDetailViewModel은 documentId를 생성자로 받음
            var detailViewModel = new DocumentDetailViewModel(vm.SelectedDocument.DocumentId);
            var detailWindow = new DocumentDetailWindow(detailViewModel);
            detailWindow.Owner = this;
            detailWindow.ShowDialog();

            // 창이 닫힌 후 문서 목록 새로고침 (상태 변경이 있을 수 있음)
            // 하지만 Confirmed 상태는 불변이므로 실제로는 변경되지 않음
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"문서 상세를 열 수 없습니다: {ex.Message}",
                "오류",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
