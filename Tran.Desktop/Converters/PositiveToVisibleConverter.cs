using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Tran.Desktop.Converters;

/// <summary>
/// 0보다 큰 숫자를 Visible로, 0 이하를 Collapsed로 변환
/// </summary>
public class PositiveToVisibleConverter : IValueConverter
{
    public static readonly PositiveToVisibleConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int intVal)
            return intVal > 0 ? Visibility.Visible : Visibility.Collapsed;
        if (value is decimal decVal)
            return decVal > 0 ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
