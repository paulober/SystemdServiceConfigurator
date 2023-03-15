using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace SystemdServiceConfigurator.Helpers
{
    internal class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
            {
                return b ? Visibility.Visible : Visibility.Collapsed;
            }

            throw new ArgumentException("Wrong type for converter. The BooleanToVisibilityConverter only supports converting from boolean to Visibility.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }

            throw new ArgumentException("Wrong type for converter. The BooleanToVisibilityConverter only supports converting from boolean to Visibility.");
        }
    }
}
