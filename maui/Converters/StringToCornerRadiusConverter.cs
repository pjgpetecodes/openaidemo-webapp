using System.Globalization;

namespace maui.Converters
{
    public class StringToCornerRadiusConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string type = value as string ?? string.Empty;
            int cornerRadius = 15;
            if (type is "ai" || type is "AI")
            {
                return new CornerRadius(cornerRadius, cornerRadius, 0, cornerRadius);
            }
            else
            {
                return new CornerRadius(cornerRadius, cornerRadius, cornerRadius, 0);
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
