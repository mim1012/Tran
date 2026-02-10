using System.Windows.Controls;
using System.Windows.Input;

namespace Tran.Desktop.Views;

/// <summary>
/// 발주 화면 (UserControl)
/// 3분할 레이아웃: 상단 좌(전체 품목) + 상단 우(자주 거래) / 하단(등록/임시저장/이력)
/// Blue 액센트 (#1E90FF)
/// </summary>
public partial class OrderView : UserControl
{
    public OrderView()
    {
        InitializeComponent();

        // TODO: ViewModel 연결 (OrderViewModel 생성 후 활성화)
        // DataContext = new ViewModels.OrderViewModel();
    }

    /// <summary>
    /// 전체 품목 그리드 더블클릭 시 발주 목록에 추가
    /// </summary>
    private void ProductGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // DataGrid에서 더블클릭된 행이 있으면 ViewModel의 AddProduct 커맨드 실행
        if (sender is DataGrid grid && grid.SelectedItem != null)
        {
            var addCommand = (DataContext as dynamic)?.AddProductCommand as ICommand;
            if (addCommand?.CanExecute(grid.SelectedItem) == true)
            {
                addCommand.Execute(grid.SelectedItem);
            }
        }
    }
}
