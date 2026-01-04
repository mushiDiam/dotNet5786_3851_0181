using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PL
{
    // Converts "Update" -> True (for IsReadOnly)
    // Converts "Add" -> False
    public class ModeToReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the button text is "Update", the field should be ReadOnly (True)
            return (string)value == "Update";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converts "Update" -> Visible
    // Converts "Add" -> Collapsed (or vice versa, depending on needs)
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Example: If we only want to show ID in Update mode
            if ((string)value == "Update")
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public class StatusToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? "✅" : "❌"; // You can also use "Active" : "Not Active"
            }
            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}