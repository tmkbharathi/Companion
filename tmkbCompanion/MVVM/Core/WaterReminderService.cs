using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using System.Media;

namespace tmkbCompanion.MVVM.Core
{
    public enum TimerState
    {
        Stopped,
        Running,
        Paused
    }

    public class WaterHistoryItem
    {
        public string Date { get; set; } = string.Empty;
        public int Target { get; set; }
        public int Consumed { get; set; }
        public double Percentage { get; set; }
    }

    public class WaterSettings
    {
        public int DailyGoal { get; set; } = 2000; // ml
        public int ReminderIntervalMinutes { get; set; } = 30;
        public int SelectedIntervalIndex { get; set; } = 1; // 0=15m, 1=30m, 2=1h, 3=Custom
        public int CustomIntervalSeconds { get; set; } = 1800; // 30 minutes in seconds
        public TimerState TimerState { get; set; } = TimerState.Stopped;
        public int RemainingTimeSeconds { get; set; } = 1800;
        public int TodayIntake { get; set; } = 0; // ml
        public string LastDrinkTime { get; set; } = "Never";
        public int RemindersSent { get; set; } = 0;
        public int DrinksLogged { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public string LastLoggedDate { get; set; } = string.Empty;
        public bool AudioAlertEnabled { get; set; } = true;
    }

    public class WaterReminderService : IDisposable
    {
        private readonly DispatcherTimer _reminderTimer;
        private WaterSettings _settings = new WaterSettings();
        private List<WaterHistoryItem> _history = new List<WaterHistoryItem>();
        private DateTime _lastMidnightCheck = DateTime.Today;

        private const string SettingsFileName = "water_settings.json";
        private const string HistoryFileName = "water_history.json";

        public event Action? SettingsChanged;
        public event Action? HistoryChanged;
        public event Action<int>? TimerTick;
        public event Action<string, string>? FallbackNotificationRequested;

        public WaterSettings Settings => _settings;
        public List<WaterHistoryItem> History => _history;

        public WaterReminderService()
        {
            LoadSettings();
            LoadHistory();

            // Set up DispatcherTimer ticking every second
            _reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _reminderTimer.Tick += Timer_Tick;

            // Start timer if it was running
            if (_settings.TimerState == TimerState.Running)
            {
                _reminderTimer.Start();
            }

            // Watch for midnight rollover
            var midnightWatcher = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            midnightWatcher.Tick += MidnightWatcher_Tick;
            midnightWatcher.Start();

            // Perform initial date validation on startup
            CheckMidnightReset();
        }

        private void LoadSettings()
        {
            try
            {
                string path = Path.Combine(AppPaths.BaseDataDirectory, SettingsFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<WaterSettings>(json);
                    if (settings != null)
                    {
                        _settings = settings;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load water settings: {ex.Message}");
            }
        }

        public void SaveSettings()
        {
            try
            {
                string path = Path.Combine(AppPaths.BaseDataDirectory, SettingsFileName);
                string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                SettingsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save water settings: {ex.Message}");
            }
        }

        private void LoadHistory()
        {
            try
            {
                string path = Path.Combine(AppPaths.BaseDataDirectory, HistoryFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var history = JsonSerializer.Deserialize<List<WaterHistoryItem>>(json);
                    if (history != null)
                    {
                        _history = history;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load water history: {ex.Message}");
            }
        }

        private void SaveHistory()
        {
            try
            {
                string path = Path.Combine(AppPaths.BaseDataDirectory, HistoryFileName);
                string json = JsonSerializer.Serialize(_history, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
                HistoryChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save water history: {ex.Message}");
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            CheckMidnightReset();

            if (_settings.TimerState == TimerState.Running)
            {
                if (_settings.RemainingTimeSeconds > 0)
                {
                    _settings.RemainingTimeSeconds--;
                    TimerTick?.Invoke(_settings.RemainingTimeSeconds);
                }
                else
                {
                    TriggerReminder();
                    ResetReminderTimer();
                }
            }
        }

        private void MidnightWatcher_Tick(object? sender, EventArgs e)
        {
            CheckMidnightReset();
        }

        private void CheckMidnightReset()
        {
            if (DateTime.Today > _lastMidnightCheck)
            {
                // Rollover occurred! Log yesterday's stats if they aren't already logged
                LogCurrentDayToHistory();

                // Check Streak logic
                string yesterdayDate = DateTime.Today.AddDays(-1).ToString("yyyy-MM-dd");
                bool yesterdayGoalReached = false;
                
                // Find if goal was reached yesterday
                var yesterdayItem = _history.Find(h => h.Date == yesterdayDate);
                if (yesterdayItem != null && yesterdayItem.Consumed >= yesterdayItem.Target)
                {
                    yesterdayGoalReached = true;
                }

                // If yesterday's goal was not reached, break the streak. Otherwise keep it.
                if (!yesterdayGoalReached)
                {
                    _settings.CurrentStreak = 0;
                }

                // Reset daily tracker
                _settings.TodayIntake = 0;
                _settings.RemindersSent = 0;
                _settings.DrinksLogged = 0;
                _settings.LastDrinkTime = "Never";

                _lastMidnightCheck = DateTime.Today;

                SaveSettings();
            }
        }

        private void LogCurrentDayToHistory()
        {
            string dateStr = _lastMidnightCheck.ToString("yyyy-MM-dd");
            
            // Check if there is already an entry for this day
            int existingIndex = _history.FindIndex(h => h.Date == dateStr);

            double percentage = _settings.DailyGoal > 0 
                ? Math.Round(((double)_settings.TodayIntake / _settings.DailyGoal) * 100, 1) 
                : 0;

            var item = new WaterHistoryItem
            {
                Date = dateStr,
                Target = _settings.DailyGoal,
                Consumed = _settings.TodayIntake,
                Percentage = percentage
            };

            if (existingIndex >= 0)
            {
                _history[existingIndex] = item;
            }
            else
            {
                // Add new history day
                _history.Insert(0, item); // Insert at top
            }

            SaveHistory();
        }

        public void LogDrink(int amount)
        {
            CheckMidnightReset();

            bool wasGoalReachedBefore = _settings.TodayIntake >= _settings.DailyGoal;

            _settings.TodayIntake += amount;
            _settings.DrinksLogged++;
            _settings.LastDrinkTime = DateTime.Now.ToString("hh:mm tt");

            bool isGoalReachedNow = _settings.TodayIntake >= _settings.DailyGoal;

            // Handle Streak Increment
            string todayDate = DateTime.Today.ToString("yyyy-MM-dd");
            if (isGoalReachedNow && !wasGoalReachedBefore)
            {
                // Streak logic: check if streak was already logged today
                if (_settings.LastLoggedDate != todayDate)
                {
                    _settings.CurrentStreak++;
                    _settings.LastLoggedDate = todayDate;
                }
            }

            // Immediately save settings and update history for today
            SaveSettings();
            LogCurrentDayToHistory();
        }

        public void StartTimer()
        {
            _settings.TimerState = TimerState.Running;
            _reminderTimer.Start();
            SaveSettings();
        }

        public void PauseTimer()
        {
            _settings.TimerState = TimerState.Paused;
            _reminderTimer.Stop();
            SaveSettings();
        }

        public void StopTimer()
        {
            _settings.TimerState = TimerState.Stopped;
            _reminderTimer.Stop();
            ResetReminderTimer();
            SaveSettings();
        }

        public void ResetReminderTimer()
        {
            _settings.RemainingTimeSeconds = GetIntervalDurationSeconds();
            TimerTick?.Invoke(_settings.RemainingTimeSeconds);
        }

        public int GetIntervalDurationSeconds()
        {
            return _settings.SelectedIntervalIndex switch
            {
                0 => 900, // 15 minutes
                1 => 1800, // 30 minutes
                2 => 3600, // 1 hour
                3 => _settings.CustomIntervalSeconds, // Custom
                _ => 1800
            };
        }

        private void TriggerReminder()
        {
            _settings.RemindersSent++;
            SaveSettings();

            // Sound Alert
            if (_settings.AudioAlertEnabled)
            {
                PlayWaterSound();
            }

            // Trigger Windows Notification
            ShowToastNotification("💧 Water Reminder", "Time to drink 250 ml of water. Keep staying hydrated!");
        }

        private void PlayWaterSound()
        {
            try
            {
                // Play system asterisk sound or default beep
                SystemSounds.Asterisk.Play();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to play reminder sound: {ex.Message}");
            }
        }

        private void ShowToastNotification(string title, string message)
        {
            try
            {
                // Simple Toast XML Template
                string toastXmlString = 
                    $"<toast><visual><binding template=\"ToastGeneric\"><text>{title}</text><text>{message}</text></binding></visual></toast>";
                
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(toastXmlString);
                
                ToastNotification toast = new ToastNotification(xmlDoc);
                
                toast.Tag = "WaterReminder";
                toast.Group = "FlowTrack";
                
                // Hook activation event to restore MainWindow
                toast.Activated += (sender, args) =>
                {
                    System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var mainWindow = System.Windows.Application.Current.MainWindow;
                        if (mainWindow != null)
                        {
                            mainWindow.Show();
                            if (mainWindow.WindowState == WindowState.Minimized)
                            {
                                mainWindow.WindowState = WindowState.Normal;
                            }
                            mainWindow.Activate();
                        }
                    }));
                };
                
                string appId = Process.GetCurrentProcess().MainModule?.FileName ?? "FlowTrack";
                ToastNotificationManager.CreateToastNotifier(appId).Show(toast);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Native Toast failed: {ex.Message}. Invoking tray balloon fallback.");
                FallbackNotificationRequested?.Invoke(title, message);
            }
        }

        public string ExportHistoryToCsv()
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Date,Target (ml),Consumed (ml),Percentage Reached (%)");

                foreach (var item in _history)
                {
                    sb.AppendLine($"{item.Date},{item.Target},{item.Consumed},{item.Percentage}");
                }

                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string filePath = Path.Combine(desktopPath, $"FlowTrack_Water_History_{DateTime.Now:yyyyMMddHHmmss}.csv");
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);

                return filePath;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"CSV Export failed: {ex.Message}");
                throw;
            }
        }

        public void ClearHistory()
        {
            _history.Clear();
            SaveHistory();
        }

        public void Dispose()
        {
            _reminderTimer.Stop();
        }
    }
}
