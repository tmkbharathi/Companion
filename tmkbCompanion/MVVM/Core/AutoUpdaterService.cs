using System;
using System.Windows;
using AutoUpdaterDotNET;
using tmkbCompanion.MVVM.ViewModel;
using tmkbCompanion.MVVM.View;

namespace tmkbCompanion.MVVM.Core
{
    public class AutoUpdaterService : IUpdateService
    {
        private readonly MainViewModel _mainViewModel;
        private readonly string _updateXmlUrl = "https://raw.githubusercontent.com/tmkbharathi/Companion/main/update.xml";
        private bool _isManualCheck;

        public AutoUpdaterService(MainViewModel mainViewModel)
        {
            _mainViewModel = mainViewModel;
            
            // Set up AutoUpdater settings
            AutoUpdater.CheckForUpdateEvent += AutoUpdater_CheckForUpdateEvent;
        }

        public void CheckForUpdates(bool isManualCheck)
        {
            _isManualCheck = isManualCheck;
            
            // AutoUpdater.Start fetches the XML in the background
            AutoUpdater.Start(_updateXmlUrl);
        }

        private void AutoUpdater_CheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            // Always run UI-related logic on the main thread
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                HandleUpdateCheckResult(args);
            }));
        }

        private void HandleUpdateCheckResult(UpdateInfoEventArgs args)
        {
            if (args.Error != null)
            {
                if (_isManualCheck)
                {
                    _mainViewModel.ShowInAppToast("Update Check Failed", "Could not fetch update details. The XML file may be missing on the server.");
                }
                return;
            }

            if (args.IsUpdateAvailable)
            {
                try
                {
                    // Enable the backdrop overlay on the main window
                    _mainViewModel.IsUpdateOverlayVisible = true;

                    var updateInfo = new UpdateInfo
                    {
                        Version = args.CurrentVersion.ToString(),
                        DownloadUrl = args.DownloadURL,
                        ChangelogUrl = args.ChangelogURL,
                        IsMandatory = args.Mandatory?.Value ?? false
                    };

                    var dialog = new UpdateDialog(updateInfo);
                    dialog.Owner = System.Windows.Application.Current.MainWindow;
                    dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    
                    // ShowDialog blocks input to owner, giving a true modal feel
                    dialog.ShowDialog();
                }
                finally
                {
                    // Disable the backdrop overlay when closed
                    _mainViewModel.IsUpdateOverlayVisible = false;
                }
            }
            else
            {
                if (_isManualCheck)
                {
                    _mainViewModel.ShowInAppToast("App Up to Date", "You are running the latest version of FlowTrack.");
                }
            }
        }
    }

    public class UpdateInfo
    {
        public string Version { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string ChangelogUrl { get; set; } = string.Empty;
        public bool IsMandatory { get; set; }
    }
}
