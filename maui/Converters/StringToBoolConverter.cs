using System.Globalization;

namespace maui.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            string type = value as string ?? string.Empty;
            if (type is "ai" || type is "AI")
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
