using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PL
{
    // Converts "Update" -> True (IsReadOnly = True)
    // Converts "Add" / Other -> False (IsReadOnly = False)
    public class ModeToReadOnlyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Safe check for string to avoid NullReferenceException
            string? mode = value as string;
            return mode == "Update";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // Converts "Update" -> Visible
    // Converts "Add" -> Collapsed
    public class ModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string? mode = value as string;

            // Logic: Show element only in Update mode
            if (mode == "Update")
            {
                return Visibility.Visible;
            }

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
                return isActive ? "✅" : "❌";
            }
            return string.Empty; 
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}