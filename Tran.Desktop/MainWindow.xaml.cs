using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;
using Tran.Data;
using Tran.Desktop.ViewModels;
using System.Collections.ObjectModel;

namespace Tran.Desktop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private ObservableCollection<DocumentViewModel> _documents = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 데이터 로드
        await LoadDocumentsAsync();
    }

    private async Task LoadDocumentsAsync()
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);

        // 문서 로드
        var documents = await context.Documents
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        // ViewModel로 변환
        _documents.Clear();
        foreach (var doc in documents)
        {
            _documents.Add(new DocumentViewModel(doc));
        }

        // DataGrid에 바인딩
        DocumentsDataGrid.ItemsSource = _documents;
    }
}