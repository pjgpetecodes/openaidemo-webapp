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
                return Color.FromArgb("#95CBEB");
            }
            else
            {
                return Color.FromArgb("#FF8AE28F");
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
