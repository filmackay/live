using System;
using System.Globalization;
using System.Windows.Data;

namespace Vertigo
{
    public class IfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var boolValue = value != null && (bool)value;
            var str = ((string) parameter).Split(new[] {','})[boolValue ? 0 : 1];
            return str;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}