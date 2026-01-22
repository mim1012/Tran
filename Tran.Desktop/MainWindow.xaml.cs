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

    /// <summary>
    /// 거래처 관리 버튼 클릭 이벤트 핸들러
    /// </summary>
    private void PartnerManagement_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);
        var viewModel = new PartnerManagementViewModel(context);
        var window = new PartnerManagementWindow(viewModel);
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// 정산 관리 버튼 클릭 이벤트 핸들러
    /// </summary>
    private void SettlementManagement_Click(object sender, RoutedEventArgs e)
    {
        var options = new DbContextOptionsBuilder<TranDbContext>()
            .UseSqlite("Data Source=tran.db")
            .Options;

        using var context = new TranDbContext(options);
        var queryService = new Tran.Data.Services.DocumentQueryService(context);
        var viewModel = new SettlementManagementViewModel(queryService);
        var window = new SettlementManagementWindow(viewModel);
        window.Owner = this;
        window.ShowDialog();
    }

    /// <summary>
    /// 양식 관리 버튼 클릭 이벤트 핸들러
    /// </summary>
    private void TemplateManagement_Click(object sender, RoutedEventArgs e)
    {
        var templateWindow = new TemplateManagementWindow();
        templateWindow.Owner = this;
        templateWindow.ShowDialog();
    }

    /// <summary>
    /// 신규 거래명세표 작성 버튼 클릭 이벤트 핸들러
    /// Draft 상태로 새 문서를 작성할 수 있는 창을 연다
    /// </summary>
    private void CreateDocumentButton_Click(object sender, RoutedEventArgs e)
    {
        var createWindow = new CreateDocumentWindow();
        createWindow.Owner = this;
        createWindow.ShowDialog();

        // 창 닫힌 후 목록 새로고침 (새 문서가 추가되었을 수 있음)
        _ = LoadDocumentsAsync();
    }

    /// <summary>
    /// 거래명세표 목록 더블클릭 이벤트 핸들러
    /// 상세 화면을 열어 문서 정보와 상태별 버튼을 표시
    /// </summary>
    private void DocumentsDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DocumentsDataGrid.SelectedItem is DocumentViewModel selectedDoc)
        {
            var detailWindow = new DocumentDetailWindow(selectedDoc.DocumentId);
            detailWindow.Owner = this;
            detailWindow.ShowDialog();

            // 창 닫힌 후 목록 새로고침 (상태 변경이 있을 수 있음)
            _ = LoadDocumentsAsync();
        }
    }
}