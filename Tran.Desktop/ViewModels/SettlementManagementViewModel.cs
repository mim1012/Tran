using System.Collections.ObjectModel;
using System.Windows.Input;
using Tran.Core.Models;
using Tran.Core.Services;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 정산 관리 ViewModel
/// READ-ONLY: 확정된 문서(Confirmed)의 집계 및 조회만 수행
/// 수정/삭제 기능 없음 - 관찰 레이어
/// </summary>
public class SettlementManagementViewModel : ViewModelBase
{
    private readonly IDocumentQueryService _queryService;

    // 기간 선택
    private DateTime _fromDate;
    private DateTime _toDate;

    // 거래처별 집계
    private ObservableCollection<SettlementSummary> _summaries;
    private SettlementSummary? _selectedSummary;

    // 선택된 거래처의 문서 목록
    private ObservableCollection<Document> _documents;
    private Document? _selectedDocument;

    // 로딩 상태
    private bool _isLoading;

    public SettlementManagementViewModel(IDocumentQueryService queryService)
    {
        _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));

        // 기본 기간: 이번 달
        _fromDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        _toDate = DateTime.Today;

        // 컬렉션 초기화
        _summaries = new ObservableCollection<SettlementSummary>();
        _documents = new ObservableCollection<Document>();

        // Commands (READ-ONLY)
        LoadSummariesCommand = new RelayCommand(async () => await LoadSummariesAsync(), () => !IsLoading);
        LoadDocumentsCommand = new RelayCommand<SettlementSummary>(async (summary) => await LoadDocumentsAsync(summary), _ => !IsLoading);
        RefreshCommand = new RelayCommand(async () => await RefreshAsync(), () => !IsLoading);
    }

    #region Properties

    public DateTime FromDate
    {
        get => _fromDate;
        set => SetProperty(ref _fromDate, value);
    }

    public DateTime ToDate
    {
        get => _toDate;
        set => SetProperty(ref _toDate, value);
    }

    public ObservableCollection<SettlementSummary> Summaries
    {
        get => _summaries;
        set => SetProperty(ref _summaries, value);
    }

    public SettlementSummary? SelectedSummary
    {
        get => _selectedSummary;
        set
        {
            if (SetProperty(ref _selectedSummary, value) && value != null)
            {
                // 자동으로 문서 목록 로드
                LoadDocumentsCommand.Execute(value);
            }
        }
    }

    public ObservableCollection<Document> Documents
    {
        get => _documents;
        set => SetProperty(ref _documents, value);
    }

    public Document? SelectedDocument
    {
        get => _selectedDocument;
        set => SetProperty(ref _selectedDocument, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    #endregion

    #region Commands (READ-ONLY)

    public ICommand LoadSummariesCommand { get; }
    public ICommand LoadDocumentsCommand { get; }
    public ICommand RefreshCommand { get; }

    #endregion

    #region Methods

    /// <summary>
    /// 거래처별 집계 로드
    /// </summary>
    public async Task LoadSummariesAsync()
    {
        try
        {
            IsLoading = true;

            var summaries = await _queryService.GetSettlementSummariesAsync(FromDate, ToDate);

            Summaries.Clear();
            foreach (var summary in summaries)
            {
                Summaries.Add(summary);
            }

            // 문서 목록 초기화
            Documents.Clear();
        }
        catch (Exception ex)
        {
            // TODO: 에러 처리 (로깅/메시지 박스)
            System.Diagnostics.Debug.WriteLine($"Error loading summaries: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 선택된 거래처의 문서 목록 로드
    /// </summary>
    private async Task LoadDocumentsAsync(SettlementSummary? summary)
    {
        if (summary == null)
            return;

        try
        {
            IsLoading = true;

            var documents = await _queryService.GetConfirmedDocumentsByCompanyAsync(
                summary.CompanyId,
                FromDate,
                ToDate);

            Documents.Clear();
            foreach (var doc in documents)
            {
                Documents.Add(doc);
            }
        }
        catch (Exception ex)
        {
            // TODO: 에러 처리
            System.Diagnostics.Debug.WriteLine($"Error loading documents: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 새로고침
    /// </summary>
    private async Task RefreshAsync()
    {
        await LoadSummariesAsync();
    }

    #endregion
}
