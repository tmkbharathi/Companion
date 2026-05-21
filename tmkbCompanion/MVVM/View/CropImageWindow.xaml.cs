using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.View
{
    public partial class CropImageWindow : Window
    {
        private string _originalImagePath;
        public string FinalImagePath { get; private set; } = string.Empty;

        public CropImageWindow(string originalImagePath)
        {
            InitializeComponent();
            _originalImagePath = originalImagePath;

            try
            {
                BitmapImage bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(originalImagePath);
                bmp.EndInit();
                PreviewBrush.ImageSource = bmp;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to load image preview: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register with the central popup manager so that pressing ESC
            // in the main window closes this popup (stack-based, LIFO order).
            PopupManager.Push(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Crop_Click(object sender, RoutedEventArgs e)
        {
            string croppedPath = CropToSquareAndSave(_originalImagePath);
            if (!string.IsNullOrEmpty(croppedPath))
            {
                FinalImagePath = croppedPath;
                DialogResult = true;
                Close();
            }
        }

        private void PlaceExact_Click(object sender, RoutedEventArgs e)
        {
            string exactPath = CopyOriginalAndSave(_originalImagePath);
            if (!string.IsNullOrEmpty(exactPath))
            {
                FinalImagePath = exactPath;
                DialogResult = true;
                Close();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private string CropToSquareAndSave(string originalPath)
        {
            try
            {
                BitmapDecoder decoder = BitmapDecoder.Create(new Uri(originalPath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapFrame frame = decoder.Frames[0];

                int width = frame.PixelWidth;
                int height = frame.PixelHeight;

                int minDim = Math.Min(width, height);
                int x = (width - minDim) / 2;
                int y = (height - minDim) / 2;

                CroppedBitmap croppedBitmap = new CroppedBitmap(frame, new Int32Rect(x, y, minDim, minDim));

                string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileData");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string extension = Path.GetExtension(originalPath);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                string destFile = Path.Combine(appDataPath, $"profile_pic_cropped_{Guid.NewGuid()}{extension}");

                using (FileStream outStream = new FileStream(destFile, FileMode.Create))
                {
                    BitmapEncoder encoder;
                    if (extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        encoder = new PngBitmapEncoder();
                    }
                    else
                    {
                        encoder = new JpegBitmapEncoder();
                    }

                    encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                    encoder.Save(outStream);
                }

                return destFile;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to crop image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }

        private string CopyOriginalAndSave(string originalPath)
        {
            try
            {
                string appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ProfileData");
                if (!Directory.Exists(appDataPath))
                {
                    Directory.CreateDirectory(appDataPath);
                }

                string extension = Path.GetExtension(originalPath);
                if (string.IsNullOrEmpty(extension)) extension = ".jpg";
                string destFile = Path.Combine(appDataPath, $"profile_pic_{Guid.NewGuid()}{extension}");

                File.Copy(originalPath, destFile, true);
                return destFile;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to copy image: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty;
            }
        }
    }
}
