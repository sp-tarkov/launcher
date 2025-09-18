/* ImageSourceConverter.cs
 * License: NCSA Open Source License
 * 
 * Copyright: SPT
 * AUTHORS:
 * waffle.lord
 */

using SPT.Launcher.Controllers;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;

namespace SPT.Launcher.Converters
{
    public class ImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is string valueString))
            {
                return null;
            }
            try
            {
                if (value is string rawUri && targetType.IsAssignableFrom(typeof(Bitmap)))
                {
                    return new Bitmap(rawUri);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
