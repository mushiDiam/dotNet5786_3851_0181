using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PL.Helpers
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
    public class EnumToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is null) return Brushes.Transparent;

            // Map by enum name. Add/remove cases for other enum values you need.
            return value.ToString() switch
            {
                // OrderStatus
                "Open" => new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xE6)),         // pale orange
                "InProgress" => new SolidColorBrush(Color.FromRgb(0xD0, 0xE9, 0xFF)),   // light blue
                "Closed" => new SolidColorBrush(Color.FromRgb(0xDF, 0xF7, 0xDF)),       // light green
                "Denied" => new SolidColorBrush(Color.FromRgb(0xFF, 0xC0, 0xC0)),       // light red
                "Cancelled" => new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE)),    // light gray

                // ScheduleStatus
                "OnTime" => new SolidColorBrush(Color.FromRgb(0xE6, 0xFF, 0xE6)),
                "Late" => new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0xB2)),
                "InRisk" => new SolidColorBrush(Color.FromRgb(0xFF, 0xF3, 0x8A)),

                // Transportation / OrderType examples
                "Car" => new SolidColorBrush(Color.FromRgb(0xD9, 0xEC, 0xFF)),
                "Motorcycle" => new SolidColorBrush(Color.FromRgb(0xF0, 0xD9, 0xFF)),
                "Bike" => new SolidColorBrush(Color.FromRgb(0xD9, 0xFF, 0xF0)),
                "Walking" => new SolidColorBrush(Color.FromRgb(0xFF, 0xF7, 0xD9)),
                "None" => new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3)),

                // Default
                _ => Brushes.Transparent,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
    [ValueConversion(typeof(object), typeof(bool))]
    public class NullToBoolConverter : IValueConverter
    {
        // returns true when value is null (i.e. editable when no active order)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}