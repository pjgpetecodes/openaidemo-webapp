using System.Globalization;

namespace maui.Converters
{
    public class StringToMarginConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string type = value as string ?? string.Empty;
            if (type is "ai" || type is "AI")
            {
                return new Thickness(0, 0, 0, 100);
            }
            else
            {
                return new Thickness(100, 0, 0, 0);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
