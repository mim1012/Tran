using System.Windows;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// 규격 입력 다이얼로그
/// DocumentItemViewModel.Specs 컬렉션을 편집
/// </summary>
public partial class SpecEditorWindow : Window
{
    private readonly SpecEditorViewModel _viewModel;

    public SpecEditorWindow(SpecEditorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        DataContext = _viewModel;

        // ViewModel의 CloseRequested 이벤트 구독
        _viewModel.CloseRequested += OnCloseRequested;
    }

    private void OnCloseRequested(object? sender, bool dialogResult)
    {
        DialogResult = dialogResult;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // 이벤트 구독 해제 (메모리 누수 방지)
        _viewModel.CloseRequested -= OnCloseRequested;
    }
}
