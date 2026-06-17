using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.View
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly HttpClient _httpClient = new HttpClient();

        public UpdateDialog(UpdateInfo updateInfo)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register with PopupManager to close on ESC
            PopupManager.Push(this);

            // Set version details
            VersionText.Text = $"Version v{_updateInfo.Version} is ready to install.";

            // If it is mandatory, disable cancellation
            if (_updateInfo.IsMandatory)
            {
                LaterButton.Visibility = Visibility.Collapsed;
                CloseButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            if (_updateInfo.IsMandatory)
            {
                // Mandatory updates cannot be skipped. Shutdown if they close the window.
                System.Windows.Application.Current.Shutdown();
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }

        private async void UpdateNow_Click(object sender, RoutedEventArgs e)
        {
            // 1. Hide buttons and show progress bar
            ButtonsPanel.Visibility = Visibility.Collapsed;
            ProgressPanel.Visibility = Visibility.Visible;
            CloseButton.Visibility = Visibility.Collapsed; // Hide X button during download

            string tempFile = Path.Combine(Path.GetTempPath(), "tmkbCompanion-setup.exe");

            try
            {
                // 2. Download asynchronously with progress tracking
                await DownloadFileWithProgressAsync(_updateInfo.DownloadUrl, tempFile);

                // 3. Launch Inno Setup installer
                Process.Start(new ProcessStartInfo
                {
                    FileName = tempFile,
                    Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
                    UseShellExecute = true
                });

                // 4. Shutdown current application so it can be updated
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to download update: {ex.Message}\n\nPlease try again later.", "Update Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Restore controls if download failed
                ButtonsPanel.Visibility = Visibility.Visible;
                ProgressPanel.Visibility = Visibility.Collapsed;
                if (!_updateInfo.IsMandatory)
                {
                    CloseButton.Visibility = Visibility.Visible;
                }
            }
        }

        private async Task DownloadFileWithProgressAsync(string downloadUrl, string destinationPath)
        {
            using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();

                long? totalBytes = response.Content.Headers.ContentLength;
                long totalBytesRead = 0;
                byte[] buffer = new byte[8192];
                bool isMoreToRead = true;

                using (var stream = await response.Content.ReadAsStreamAsync())
                using (var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                {
                    do
                    {
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            isMoreToRead = false;
                            UpdateProgress(totalBytesRead, totalBytes);
                            continue;
                        }

                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                        UpdateProgress(totalBytesRead, totalBytes);
                    }
                    while (isMoreToRead);
                }
            }
        }

        private void UpdateProgress(long bytesRead, long? totalBytes)
        {
            if (totalBytes == null || totalBytes.Value <= 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    PercentText.Text = "Downloading...";
                    DownloadProgressBar.IsIndeterminate = true;
                }));
                return;
            }

            double percentage = (double)bytesRead / totalBytes.Value * 100.0;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = percentage;
                PercentText.Text = $"{(int)percentage}%";
            }));
        }
    }
}
