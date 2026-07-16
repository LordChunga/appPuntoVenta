using System;
using System.Globalization;
using System.Windows.Data;

namespace MiniPosWpf.Converters;

public class BoolToIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isTrue)
        {
            return isTrue ? 1 : 0;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int index)
        {
            return index == 1;
        }
        return false;
    }
}
