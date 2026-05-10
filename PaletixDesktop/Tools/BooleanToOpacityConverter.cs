using System;
using Microsoft.UI.Xaml.Data;

namespace PaletixDesktop.Converters
{
    public sealed class BooleanToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var whenTrue = 0.62d;
            var whenFalse = 1d;

            if (parameter is string text && double.TryParse(text, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                whenTrue = parsed;
            }

            return value is bool boolean && boolean ? whenTrue : whenFalse;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is double opacity && opacity < 1d;
        }
    }
}
