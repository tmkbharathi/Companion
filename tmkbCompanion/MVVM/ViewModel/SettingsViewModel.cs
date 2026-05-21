using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly DashboardViewModel _dashboard;
        private bool _launchOnStartup;
        private bool _showNotifications = true;
        private bool _darkMode = true;
        private double _cacheProgress = 0.5;
        private string _cacheUsedText = "Calculating...";
        private string _accentColorHex = "#5b8cff";

        // Max capacity shown in the progress bar (MB)
        private const double MaxCacheMB = 100.0;

        private const string SettingsFileName = "app_settings.json";

        public DashboardViewModel Dashboard => _dashboard;

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
            set
            {
                if (SetProperty(ref _showNotifications, value))
                {
                    SaveSettings();
                }
            }
        }

        public bool DarkMode
        {
            get => _darkMode;
            set
            {
                if (SetProperty(ref _darkMode, value))
                {
                    SaveSettings();
                }
            }
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

        public SettingsViewModel(DashboardViewModel dashboard)
        {
            _dashboard = dashboard;
            SetAccentColorCommand = new RelayCommand(SetAccentColor);
            CleanLogsCommand = new RelayCommand(CleanLogs);
            _launchOnStartup = GetStartupRegistryState();

            LoadSettings();

            // Calculate real cache usage from disk
            RefreshCacheStats();
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
                ApplyAccentColor(colorHex);
                SaveSettings();
            }
        }

        private void ApplyAccentColor(string colorHex)
        {
            try
            {
                var color = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(colorHex);
                
                // Update resources globally in the application
                System.Windows.Application.Current.Resources["AccentColor"] = color;
                System.Windows.Application.Current.Resources["AccentBrush"] = new System.Windows.Media.SolidColorBrush(color);

                _accentColorHex = colorHex;

                // Raise event to notify other services (like TrayIconManager)
                AccentColorChanged?.Invoke(color);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to parse and set accent color: {ex.Message}");
            }
        }

        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = JsonSerializer.Deserialize<AppSettingsData>(json);
                    if (data != null)
                    {
                        _showNotifications = data.ShowNotifications;
                        _darkMode = data.DarkMode;
                        ApplyAccentColor(data.AccentColorHex);
                    }
                }
                else
                {
                    ApplyAccentColor("#5b8cff");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load settings: {ex.Message}");
                ApplyAccentColor("#5b8cff");
            }
        }

        private void SaveSettings()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
                var data = new AppSettingsData
                {
                    AccentColorHex = _accentColorHex,
                    ShowNotifications = ShowNotifications,
                    DarkMode = DarkMode
                };
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save settings: {ex.Message}");
            }
        }

        private void CleanLogs()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Delete the notes file (user-generated cache)
                string notesPath = Path.Combine(baseDir, "flowtrack_notes.txt");
                if (File.Exists(notesPath))
                    File.Delete(notesPath);

                // Also clear the dashboard notes text if it's still loaded
                _dashboard.NotesText = string.Empty;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CleanLogs failed: {ex.Message}");
            }

            // Refresh the display with real numbers after cleaning
            RefreshCacheStats();
        }

        /// <summary>
        /// Measures the actual on-disk size of all app cache/data files and
        /// updates <see cref="CacheProgress"/> and <see cref="CacheUsedText"/>.
        /// </summary>
        private void RefreshCacheStats()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                long totalBytes = 0;

                // JSON settings + data files
                string[] dataFiles =
                {
                    "profile_settings.json",
                    "app_settings.json",
                    "link_settings.json",
                    "flowtrack_notes.txt"
                };

                foreach (var file in dataFiles)
                {
                    string path = Path.Combine(baseDir, file);
                    if (File.Exists(path))
                        totalBytes += new FileInfo(path).Length;
                }

                // Profile images stored under ProfileData/
                string profileDataDir = Path.Combine(baseDir, "ProfileData");
                if (Directory.Exists(profileDataDir))
                {
                    foreach (var file in Directory.GetFiles(profileDataDir))
                        totalBytes += new FileInfo(file).Length;
                }

                double usedMB = totalBytes / (1024.0 * 1024.0);
                double progress = Math.Min((usedMB / MaxCacheMB) * 100.0, 100.0);

                // Always show at least a 0.5% sliver so the bar is visible
                CacheProgress = Math.Max(progress, 0.5);

                if (usedMB < 1.0)
                {
                    double usedKB = totalBytes / 1024.0;
                    CacheUsedText = $"{usedKB:F1} KB of {MaxCacheMB:F0} MB";
                }
                else
                {
                    CacheUsedText = $"{usedMB:F2} MB of {MaxCacheMB:F0} MB";
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RefreshCacheStats failed: {ex.Message}");
                CacheUsedText = "Unavailable";
                CacheProgress = 0.5;
            }
        }
    }

    public class AppSettingsData
    {
        public string AccentColorHex { get; set; } = "#5b8cff";
        public bool ShowNotifications { get; set; } = true;
        public bool DarkMode { get; set; } = true;
    }
}
