using System.Windows;
using MahApps.Metro.Controls;
using Tran.Desktop.ViewModels;

namespace Tran.Desktop;

/// <summary>
/// CreateDocumentWindow.xaml에 대한 상호 작용 논리
/// </summary>
public partial class CreateDocumentWindow : MetroWindow
{
    public CreateDocumentWindow()
    {
        InitializeComponent();

        // ViewModel 설정
        DataContext = new CreateDocumentViewModel();
    }
}
