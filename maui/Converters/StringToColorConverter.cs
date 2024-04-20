using System.Globalization;

namespace maui.Converters
{
    public class StringToColorConverter :IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string type = value as string ?? string.Empty;
            if (type is "ai" || type is "AI")
            {
                return Color.FromArgb("#550082cf");
            }
            else
            {
                return Color.FromArgb("#558ae28a");
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
