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
                PreviewImage.Source = bmp;

                // Set initial size of the image control to maintain aspect ratio and fill the 240x240 container (UniformToFill behavior)
                double originalWidth = bmp.PixelWidth;
                double originalHeight = bmp.PixelHeight;
                double containerWidth = 240;
                double containerHeight = 240;

                double dispW, dispH;
                if (originalWidth / originalHeight > containerWidth / containerHeight)
                {
                    dispH = containerHeight;
                    dispW = containerHeight * (originalWidth / originalHeight);
                }
                else
                {
                    dispW = containerWidth;
                    dispH = containerWidth * (originalHeight / originalWidth);
                }

                PreviewImage.Width = dispW;
                PreviewImage.Height = dispH;
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

        private System.Windows.Point _startPoint;
        private System.Windows.Point _origin;
        private bool _isDragging = false;

        private void ImageGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement element)
            {
                _isDragging = true;
                _startPoint = e.GetPosition(element);
                _origin = new System.Windows.Point(ImageTranslate.X, ImageTranslate.Y);
                element.CaptureMouse();
            }
        }

        private void ImageGrid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && sender is UIElement element)
            {
                System.Windows.Point currentPoint = e.GetPosition(element);
                double dx = currentPoint.X - _startPoint.X;
                double dy = currentPoint.Y - _startPoint.Y;

                ImageTranslate.X = _origin.X + dx;
                ImageTranslate.Y = _origin.Y + dy;
            }
        }

        private void ImageGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging && sender is UIElement element)
            {
                _isDragging = false;
                element.ReleaseMouseCapture();
            }
        }

        private void ImageGrid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double scaleChange = e.Delta > 0 ? 1.1 : 0.9;
            double newScale = ImageScale.ScaleX * scaleChange;

            // Limit scale between 0.5 and 10
            if (newScale >= 0.5 && newScale <= 10)
            {
                ImageScale.ScaleX = newScale;
                ImageScale.ScaleY = newScale;
            }
        }

        private string CropToSquareAndSave(string originalPath)
        {
            try
            {
                BitmapDecoder decoder = BitmapDecoder.Create(new Uri(originalPath), BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                BitmapFrame frame = decoder.Frames[0];

                double originalWidth = frame.PixelWidth;
                double originalHeight = frame.PixelHeight;

                // Container size is 240x240 (Preview size inside circular border)
                double containerWidth = 240;
                double containerHeight = 240;

                // Calculate initial uniform fit size
                double dispW, dispH;
                if (originalWidth / originalHeight > containerWidth / containerHeight)
                {
                    dispH = containerHeight;
                    dispW = containerHeight * (originalWidth / originalHeight);
                }
                else
                {
                    dispW = containerWidth;
                    dispH = containerWidth * (originalHeight / originalWidth);
                }

                double S = ImageScale.ScaleX;
                double Tx = ImageTranslate.X;
                double Ty = ImageTranslate.Y;

                double scaledW = dispW * S;
                double scaledH = dispH * S;

                double left = (containerWidth - scaledW) / 2 + Tx;
                double top = (containerHeight - scaledH) / 2 + Ty;

                double scaleFactorX = scaledW / originalWidth;
                double scaleFactorY = scaledH / originalHeight;

                double cropX_original = -left / scaleFactorX;
                double cropY_original = -top / scaleFactorY;
                double cropW_original = containerWidth / scaleFactorX;
                double cropH_original = containerHeight / scaleFactorY;

                int x = (int)Math.Max(0, Math.Min(originalWidth - 10, cropX_original));
                int y = (int)Math.Max(0, Math.Min(originalHeight - 10, cropY_original));
                int w = (int)Math.Max(10, Math.Min(originalWidth - x, cropW_original));
                int h = (int)Math.Max(10, Math.Min(originalHeight - y, cropH_original));

                // Crop as a square (using minimum of w and h to ensure it is square)
                int minDim = Math.Min(w, h);

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
