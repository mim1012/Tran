using System.Windows;
using MahApps.Metro.Controls;

namespace Tran.Desktop.Views;

/// <summary>
/// 품목 선택 팝업 (모달 Window)
/// 품목 검색 + 체크 선택 + 수량 입력 후 일괄 추가
/// </summary>
public partial class ProductSelectionPopup : MetroWindow
{
    public ProductSelectionPopup()
    {
        InitializeComponent();

        DataContext = new ViewModels.ProductSelectionPopupViewModel();

        // 검색 텍스트박스에 포커스
        Loaded += (s, e) => SearchTextBox.Focus();
    }

    /// <summary>
    /// Owner 윈도우를 설정하는 편의 생성자
    /// </summary>
    public ProductSelectionPopup(Window owner) : this()
    {
        Owner = owner;
    }
}
