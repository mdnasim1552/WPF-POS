using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace PosManagement.Services
{
    public class ProductImageConverter : IValueConverter
    {
        private readonly ProductImageService _imageService = new();

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            if (value is not string imagePath ||
                string.IsNullOrWhiteSpace(imagePath))
            {
                return Binding.DoNothing;
            }

            return _imageService.LoadImage(imagePath)
                ?? Binding.DoNothing;
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
