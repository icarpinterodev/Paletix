using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace PaletixDesktop.Converters
{
    // BooleanToVisibilityConverter converts a boolean value to a Visibility enumeration value to control the visibility of UI elements in XAML.
    // It also supports an optional parameter to invert the logic.
    public sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var visible = value is bool boolean && boolean;
            if (parameter is string text && text.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                visible = !visible;
            }

            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value is Visibility visibility && visibility == Visibility.Visible;
        }
    }
}
