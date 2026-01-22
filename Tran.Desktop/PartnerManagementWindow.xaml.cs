using System.Windows;
using Tran.Data;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// 거래처 관리 화면
/// MVVM 패턴: ViewModel에 비즈니스 로직 위임
/// </summary>
public partial class PartnerManagementWindow : Window
{
    private readonly PartnerManagementViewModel _viewModel;

    public PartnerManagementWindow(PartnerManagementViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        // 초기 로딩 후 카운트 업데이트
        Loaded += async (s, e) =>
        {
            await Task.Delay(100); // ViewModel 로딩 대기
            UpdateCounts();

            // ViewModel의 Companies 변경 시 카운트 업데이트
            _viewModel.Companies.CollectionChanged += (s, e) => UpdateCounts();
        };
    }

    /// <summary>
    /// 활성/전체 거래처 카운트 업데이트
    /// </summary>
    private void UpdateCounts()
    {
        var total = _viewModel.Companies.Count;
        var active = _viewModel.Companies.Count(c => c.IsActive);

        Dispatcher.Invoke(() =>
        {
            ActiveCountText.Text = active.ToString();
            TotalCountText.Text = total.ToString();
        });
    }

    /// <summary>
    /// 닫기 버튼 클릭
    /// </summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
