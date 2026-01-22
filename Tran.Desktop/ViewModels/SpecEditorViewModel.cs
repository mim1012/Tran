using System.Collections.ObjectModel;
using System.Windows.Input;
using Tran.Core.Models;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 규격 입력 다이얼로그의 ViewModel
/// DocumentItemViewModel.Specs를 직접 참조하여 편집
/// </summary>
public class SpecEditorViewModel : ViewModelBase
{
    private readonly DocumentItemViewModel _parentItem;

    /// <summary>
    /// 다이얼로그 닫기 요청 이벤트
    /// (bool: DialogResult)
    /// </summary>
    public event EventHandler<bool>? CloseRequested;

    public SpecEditorViewModel(DocumentItemViewModel parentItem)
    {
        _parentItem = parentItem ?? throw new ArgumentNullException(nameof(parentItem));

        // 부모 ViewModel의 Specs 컬렉션을 직접 참조
        // (복사본이 아니므로 실시간 반영됨)
        Specs = _parentItem.Specs;

        AddSpecCommand = new RelayCommand(ExecuteAddSpec);
        RemoveSpecCommand = new RelayCommand<SpecEntry>(ExecuteRemoveSpec);
        OkCommand = new RelayCommand(ExecuteOk);
        CancelCommand = new RelayCommand(ExecuteCancel);
    }

    /// <summary>
    /// 규격 컬렉션 (부모 DocumentItemViewModel의 Specs 직접 참조)
    /// </summary>
    public ObservableCollection<SpecEntry> Specs { get; }

    /// <summary>
    /// 항목 추가 명령
    /// </summary>
    public ICommand AddSpecCommand { get; }

    /// <summary>
    /// 항목 삭제 명령
    /// </summary>
    public ICommand RemoveSpecCommand { get; }

    /// <summary>
    /// 확인 명령
    /// </summary>
    public ICommand OkCommand { get; }

    /// <summary>
    /// 취소 명령
    /// </summary>
    public ICommand CancelCommand { get; }

    private void ExecuteAddSpec()
    {
        var newSpec = new SpecEntry
        {
            Key = string.Empty,
            Value = string.Empty
        };

        Specs.Add(newSpec);

        // 부모 ViewModel의 UI 갱신
        _parentItem.RefreshSpecProperties();
    }

    private void ExecuteRemoveSpec(SpecEntry? spec)
    {
        if (spec == null) return;

        Specs.Remove(spec);

        // 부모 ViewModel의 UI 갱신
        _parentItem.RefreshSpecProperties();
    }

    private void ExecuteOk()
    {
        // 빈 Key나 Value를 가진 항목 제거 (Canonical 규칙 적용)
        var emptySpecs = Specs.Where(s => string.IsNullOrWhiteSpace(s.Key) || string.IsNullOrWhiteSpace(s.Value)).ToList();
        foreach (var empty in emptySpecs)
        {
            Specs.Remove(empty);
        }

        // 부모 ViewModel의 UI 갱신
        _parentItem.RefreshSpecProperties();

        // 다이얼로그 닫기 (DialogResult = true)
        CloseRequested?.Invoke(this, true);
    }

    private void ExecuteCancel()
    {
        // 변경 사항이 이미 ObservableCollection에 반영되어 있으므로
        // 취소 시 원복이 필요하면 별도 로직 필요
        // 현재는 단순히 닫기만 수행
        CloseRequested?.Invoke(this, false);
    }
}
