using System.Globalization;

namespace maui.Converters
{
    public class StringToCornerRadiusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string type = value as string ?? string.Empty;
            if (type is "ai" || type is "AI")
            {
                return new CornerRadius(10, 10, 0, 10);
            }
            else
            {
                return new CornerRadius(10, 10, 10, 0);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
