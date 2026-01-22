using System.Collections.ObjectModel;
using Tran.Core.Models;

namespace Tran.Desktop.ViewModels;

/// <summary>
/// 품목 입력을 위한 ViewModel
/// DataGrid 바인딩 및 자동 계산 기능 제공
/// </summary>
public class DocumentItemViewModel : ViewModelBase
{
    private string _itemName = string.Empty;
    private string _optionText = string.Empty;
    private decimal _quantity = 1;
    private decimal _unitPrice = 0;

    /// <summary>
    /// 품명 (DocumentItem.ItemName에 매핑)
    /// </summary>
    public string ItemName
    {
        get => _itemName;
        set
        {
            if (SetProperty(ref _itemName, value))
            {
                RaisePropertyChanged(nameof(LineAmount));
            }
        }
    }

    /// <summary>
    /// 옵션/비고 (DocumentItem.OptionText에 매핑)
    /// </summary>
    public string OptionText
    {
        get => _optionText;
        set => SetProperty(ref _optionText, value);
    }

    /// <summary>
    /// 수량
    /// </summary>
    public decimal Quantity
    {
        get => _quantity;
        set
        {
            if (SetProperty(ref _quantity, value))
            {
                RaisePropertyChanged(nameof(LineAmount));
            }
        }
    }

    /// <summary>
    /// 단가
    /// </summary>
    public decimal UnitPrice
    {
        get => _unitPrice;
        set
        {
            if (SetProperty(ref _unitPrice, value))
            {
                RaisePropertyChanged(nameof(LineAmount));
            }
        }
    }

    /// <summary>
    /// 라인 금액 (자동 계산: 수량 × 단가)
    /// DocumentItem.LineAmount에 매핑
    /// </summary>
    public decimal LineAmount => Quantity * UnitPrice;

    /// <summary>
    /// 규격 컬렉션
    /// DocumentItem.ExtraDataJson에 저장됨
    /// </summary>
    public ObservableCollection<SpecEntry> Specs { get; } = new();

    /// <summary>
    /// 규격 개수 (UI 바인딩용)
    /// </summary>
    public int SpecCount => Specs.Count;

    /// <summary>
    /// 규격 요약 텍스트 (버튼 표시용)
    /// </summary>
    public string SpecSummary
    {
        get
        {
            if (Specs.Count == 0) return "규격 입력";
            return $"규격 {Specs.Count}";
        }
    }

    /// <summary>
    /// 규격 툴팁 (마우스 오버 시 표시)
    /// </summary>
    public string SpecTooltip
    {
        get
        {
            if (Specs.Count == 0) return "규격을 입력하세요";
            return string.Join("\n", Specs.Select(s => $"{s.Key}: {s.Value}"));
        }
    }

    /// <summary>
    /// 규격 변경 시 UI 갱신
    /// </summary>
    public void RefreshSpecProperties()
    {
        RaisePropertyChanged(nameof(SpecCount));
        RaisePropertyChanged(nameof(SpecSummary));
        RaisePropertyChanged(nameof(SpecTooltip));
    }
}
