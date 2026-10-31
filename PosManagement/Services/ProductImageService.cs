using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace PosManagement.Services
{
    public class ProductImageService
    {
        private const int MaxImageSize = 500;

        private readonly string _imageDirectory;

        public ProductImageService()
        {
            _imageDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "PosManagement",
                "ProductImages");

            Directory.CreateDirectory(_imageDirectory);
        }

        public string SaveProductImage(string sourcePath, int productId)
        {
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Selected image was not found.");

            var fileName = $"{productId}_{Guid.NewGuid():N}.jpg";
            var destinationPath = Path.Combine(_imageDirectory, fileName);

            try
            {
                using var stream = new FileStream(
                    sourcePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var decoder = BitmapDecoder.Create(
                    stream,
                    BitmapCreateOptions.PreservePixelFormat,
                    BitmapCacheOption.OnLoad);

                var frame = decoder.Frames[0];

                if (frame.PixelWidth <= 0 || frame.PixelHeight <= 0)
                    throw new InvalidOperationException("Invalid image dimensions.");

                double scale = Math.Min(
                    1.0,
                    (double)MaxImageSize /
                    Math.Max(frame.PixelWidth, frame.PixelHeight));

                BitmapSource image = frame;

                if (scale < 1.0)
                {
                    image = new TransformedBitmap(
                        frame,
                        new System.Windows.Media.ScaleTransform(scale, scale));
                }

                var encoder = new JpegBitmapEncoder
                {
                    QualityLevel = 85
                };

                encoder.Frames.Add(BitmapFrame.Create(image));

                using var output = new FileStream(
                    destinationPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None);

                encoder.Save(output);

                return Path.Combine("ProductImages", fileName);
            }
            catch
            {
                if (File.Exists(destinationPath))
                    File.Delete(destinationPath);

                throw;
            }
        }

        public BitmapImage? LoadImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return null;

            try
            {
                string absolutePath = GetAbsolutePath(imagePath);

                if (!File.Exists(absolutePath))
                    return null;

                var bitmap = new BitmapImage();

                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(absolutePath);
                bitmap.EndInit();
                bitmap.Freeze();

                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public string GetAbsolutePath(string imagePath)
        {
            if (Path.IsPathRooted(imagePath))
                return imagePath;

            return Path.Combine(_imageDirectory, Path.GetFileName(imagePath));
        }

        public void DeleteProductImage(string? imagePath)
        {
            if (string.IsNullOrWhiteSpace(imagePath))
                return;

            try
            {
                string absolutePath = GetAbsolutePath(imagePath);

                if (File.Exists(absolutePath))
                    File.Delete(absolutePath);
            }
            catch
            {
                // Image cleanup should not break product operations.
            }
        }
    }
}
