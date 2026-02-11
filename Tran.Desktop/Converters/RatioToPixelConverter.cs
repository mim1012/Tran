using System.Globalization;
using System.Windows.Data;

namespace Tran.Desktop.Converters;

/// <summary>
/// ratio(0.0~1.0) * maxWidth(parameter) → pixel width
/// 바차트 Border Width 바인딩용
/// </summary>
public class RatioToPixelConverter : IValueConverter
{
    public static readonly RatioToPixelConverter Instance = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double ratio && double.TryParse(parameter?.ToString(), out var max))
            return Math.Max(0, ratio * max);
        return 0.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
