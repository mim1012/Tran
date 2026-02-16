using System.Windows;
using MahApps.Metro.Controls;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// DocumentDetailWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class DocumentDetailWindow : MetroWindow
{
    public DocumentDetailWindow(string documentId)
    {
        InitializeComponent();

        // ViewModel 설정
        DataContext = new DocumentDetailViewModel(documentId);
    }

    /// <summary>
    /// ViewModel을 직접 받는 생성자 (정산 관리 등에서 사용)
    /// </summary>
    public DocumentDetailWindow(DocumentDetailViewModel viewModel)
    {
        InitializeComponent();

        // ViewModel 설정
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
