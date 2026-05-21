using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private bool _launchOnStartup;
        private bool _showNotifications = true;
        private bool _darkMode = true;
        private double _cacheProgress = 24.0; // 1.2 GB of 5.0 GB (24%)
        private string _cacheUsedText = "1.2 GB of 5.0 GB used";

        public event Action<System.Windows.Media.Color>? AccentColorChanged;

        public bool LaunchOnStartup
        {
            get => _launchOnStartup;
            set
            {
                if (SetProperty(ref _launchOnStartup, value))
                {
                    SetStartupRegistry(value);
                }
            }
        }

        public bool ShowNotifications
        {
            get => _showNotifications;
            set => SetProperty(ref _showNotifications, value);
        }

        public bool DarkMode
        {
            get => _darkMode;
            set => SetProperty(ref _darkMode, value);
        }

        public double CacheProgress
        {
            get => _cacheProgress;
            set => SetProperty(ref _cacheProgress, value);
        }

        public string CacheUsedText
        {
            get => _cacheUsedText;
            set => SetProperty(ref _cacheUsedText, value);
        }

        public ICommand SetAccentColorCommand { get; }
        public ICommand CleanLogsCommand { get; }

        public SettingsViewModel()
        {
            SetAccentColorCommand = new RelayCommand(SetAccentColor);
            CleanLogsCommand = new RelayCommand(CleanLogs);
            _launchOnStartup = GetStartupRegistryState();
        }

        private bool GetStartupRegistryState()
        {
            try
            {
                string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(path, false))
                {
                    return key?.GetValue("FlowTrack") != null;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read registry startup: {ex.Message}");
                return false;
            }
        }

        private void SetStartupRegistry(bool startOnBoot)
        {
            try
            {
                string path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(path, true))
                {
                    if (key != null)
                    {
                        if (startOnBoot)
                        {
                            string executablePath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                            if (!string.IsNullOrEmpty(executablePath))
                            {
                                key.SetValue("FlowTrack", executablePath);
                            }
                        }
                        else
                        {
                            key.DeleteValue("FlowTrack", false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update registry startup: {ex.Message}");
            }
        }

        private void SetAccentColor(object? parameter)
        {
            if (parameter is string colorHex)
            {
                try
                {
                    var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                    
                    // Update resources globally in the application
                    System.Windows.Application.Current.Resources["AccentColor"] = color;
                    System.Windows.Application.Current.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);

                    // Raise event to notify other services (like TrayIconManager)
                    AccentColorChanged?.Invoke(color);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to parse and set accent color: {ex.Message}");
                }
            }
        }

        private void CleanLogs()
        {
            // Simulate log clean-up
            CacheProgress = 2.0; // 0.1 GB / 5.0 GB (2%)
            CacheUsedText = "0.1 GB of 5.0 GB used";
        }
    }
}
