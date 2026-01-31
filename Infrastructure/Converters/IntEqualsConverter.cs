using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace EasyLingo.Infrastructure.Converters
{
    public class IntEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 2)
                return false;

            var v0 = values[0];
            var v1 = values[1];

            if (v0 == null || v0 == DependencyProperty.UnsetValue)
                return false;

            if (v1 == null || v1 == DependencyProperty.UnsetValue)
                return false;

            int setId;
            if (!TryToInt(v0, out setId))
                return false;

            int selectedId;
            if (!TryToInt(v1, out selectedId))
                return false;

            return setId == selectedId;
        }

        private bool TryToInt(object value, out int result)
        {
            result = 0;

            if (value == null)
                return false;

            if (value is int)
            {
                result = (int)value;
                return true;
            }

            var s = value as string;
            if (s != null)
                return int.TryParse(s, out result);

            try
            {
                result = System.Convert.ToInt32(value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
