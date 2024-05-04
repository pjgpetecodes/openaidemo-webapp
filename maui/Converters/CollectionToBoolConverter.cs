using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace maui.Converters
{
    public class CollectionToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // If the value is a collection and is not null, return true
            if (value is System.Collections.ICollection collection)
            {
                return collection != null && collection.Count > 0;
            }

            return value != null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
