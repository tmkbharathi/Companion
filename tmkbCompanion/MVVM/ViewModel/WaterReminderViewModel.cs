using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class WaterReminderViewModel : ViewModelBase
    {
        private readonly WaterReminderService _service;
        private string _customDrinkAmount = "250";
        private int _customIntervalMinutes = 30;
        private string _countdownText = "30:00";
        private string _motivationalQuote = string.Empty;

        public WaterReminderService Service => _service;

        // View bindings
        public int DailyGoal
        {
            get => _service.Settings.DailyGoal;
            set
            {
                if (value < 100) value = 100;
                if (value > 10000) value = 10000;
                _service.Settings.DailyGoal = value;
                _service.SaveSettings();
                RefreshStats();
            }
        }

        public int TodayIntake => _service.Settings.TodayIntake;

        public int RemainingWater => Math.Max(0, DailyGoal - TodayIntake);

        public double ProgressPercentage => DailyGoal > 0 
            ? Math.Min(100.0, Math.Round(((double)TodayIntake / DailyGoal) * 100, 1)) 
            : 0;

        public string DisplayPercentage => $"{ProgressPercentage:F1}%";

        // Bottle Fill height percentage (0.0 to 1.0)
        public double BottleFillRatio => ProgressPercentage / 100.0;

        public int SelectedIntervalIndex
        {
            get => _service.Settings.SelectedIntervalIndex;
            set
            {
                _service.Settings.SelectedIntervalIndex = value;
                // If custom is not selected, set interval automatically
                if (value != 3)
                {
                    _service.Settings.ReminderIntervalMinutes = value switch
                    {
                        0 => 15,
                        1 => 30,
                        2 => 60,
                        _ => 30
                    };
                }
                _service.ResetReminderTimer();
                _service.SaveSettings();
                OnPropertyChanged();
                OnPropertyChanged(nameof(CountdownText));
            }
        }

        public int CustomIntervalMinutes
        {
            get => _customIntervalMinutes;
            set
            {
                if (value < 1) value = 1;
                if (value > 1440) value = 1440; // 24 hours
                if (SetProperty(ref _customIntervalMinutes, value))
                {
                    _service.Settings.CustomIntervalSeconds = value * 60;
                    if (SelectedIntervalIndex == 3)
                    {
                        _service.ResetReminderTimer();
                        _service.SaveSettings();
                    }
                }
            }
        }

        public string CustomDrinkAmount
        {
            get => _customDrinkAmount;
            set => SetProperty(ref _customDrinkAmount, value);
        }

        public string CountdownText
        {
            get => _countdownText;
            set => SetProperty(ref _countdownText, value);
        }

        public string LastDrinkTime => _service.Settings.LastDrinkTime;
        public int RemindersSent => _service.Settings.RemindersSent;
        public int DrinksLogged => _service.Settings.DrinksLogged;
        public int CurrentStreak => _service.Settings.CurrentStreak;

        public bool AudioAlertEnabled
        {
            get => _service.Settings.AudioAlertEnabled;
            set
            {
                _service.Settings.AudioAlertEnabled = value;
                _service.SaveSettings();
                OnPropertyChanged();
            }
        }

        public bool IsTimerRunning => _service.Settings.TimerState == TimerState.Running;
        public bool IsTimerStopped => _service.Settings.TimerState == TimerState.Stopped;
        public bool IsTimerPaused => _service.Settings.TimerState == TimerState.Paused;

        public string MotivationalQuote
        {
            get => _motivationalQuote;
            set => SetProperty(ref _motivationalQuote, value);
        }

        // Achievements
        public bool Is50PercentBadgeUnlocked => ProgressPercentage >= 50;
        public bool Is75PercentBadgeUnlocked => ProgressPercentage >= 75;
        public bool Is100PercentBadgeUnlocked => ProgressPercentage >= 100;

        public ObservableCollection<WaterHistoryItem> HistoryList { get; }

        // Commands
        public ICommand AddWaterCommand { get; }
        public ICommand StartTimerCommand { get; }
        public ICommand PauseTimerCommand { get; }
        public ICommand StopTimerCommand { get; }
        public ICommand ExportCsvCommand { get; }

        public WaterReminderViewModel(WaterReminderService service)
        {
            _service = service;

            // Load initial collections
            HistoryList = new ObservableCollection<WaterHistoryItem>(_service.History);
            _customIntervalMinutes = _service.Settings.CustomIntervalSeconds / 60;

            // Bind Commands
            AddWaterCommand = new RelayCommand(AddWater);
            StartTimerCommand = new RelayCommand(StartTimer);
            PauseTimerCommand = new RelayCommand(PauseTimer);
            StopTimerCommand = new RelayCommand(StopTimer);
            ExportCsvCommand = new RelayCommand(ExportCsv);

            // Wire Service Events
            _service.SettingsChanged += Service_SettingsChanged;
            _service.HistoryChanged += Service_HistoryChanged;
            _service.TimerTick += Service_TimerTick;

            // Initialize display states
            UpdateCountdownText(_service.Settings.RemainingTimeSeconds);
            RefreshStats();
        }

        private void Service_SettingsChanged()
        {
            RefreshStats();
        }

        private void Service_HistoryChanged()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                HistoryList.Clear();
                foreach (var item in _service.History)
                {
                    HistoryList.Add(item);
                }
            });
        }

        private void Service_TimerTick(int remainingSeconds)
        {
            UpdateCountdownText(remainingSeconds);
        }

        private void UpdateCountdownText(int seconds)
        {
            int mins = seconds / 60;
            int secs = seconds % 60;
            CountdownText = $"{mins:D2}:{secs:D2}";
        }

        private void AddWater(object? parameter)
        {
            int amount = 250; // default
            if (parameter is string amtStr && int.TryParse(amtStr, out int paramAmt))
            {
                amount = paramAmt;
            }
            else if (parameter == null)
            {
                // Use custom input
                int.TryParse(CustomDrinkAmount, out amount);
            }

            if (amount <= 0) return;

            _service.LogDrink(amount);
            RefreshStats();
        }

        private void StartTimer()
        {
            _service.StartTimer();
            RefreshTimerStates();
        }

        private void PauseTimer()
        {
            _service.PauseTimer();
            RefreshTimerStates();
        }

        private void StopTimer()
        {
            _service.StopTimer();
            RefreshTimerStates();
        }

        private void ExportCsv()
        {
            try
            {
                string filePath = _service.ExportHistoryToCsv();
                System.Windows.MessageBox.Show($"Water history successfully exported to Desktop:\n{System.IO.Path.GetFileName(filePath)}", "Export Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Failed to export history: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshTimerStates()
        {
            OnPropertyChanged(nameof(IsTimerRunning));
            OnPropertyChanged(nameof(IsTimerStopped));
            OnPropertyChanged(nameof(IsTimerPaused));
        }

        private void RefreshStats()
        {
            OnPropertyChanged(nameof(DailyGoal));
            OnPropertyChanged(nameof(TodayIntake));
            OnPropertyChanged(nameof(RemainingWater));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(DisplayPercentage));
            OnPropertyChanged(nameof(BottleFillRatio));
            OnPropertyChanged(nameof(LastDrinkTime));
            OnPropertyChanged(nameof(RemindersSent));
            OnPropertyChanged(nameof(DrinksLogged));
            OnPropertyChanged(nameof(CurrentStreak));
            OnPropertyChanged(nameof(SelectedIntervalIndex));

            // Badges
            OnPropertyChanged(nameof(Is50PercentBadgeUnlocked));
            OnPropertyChanged(nameof(Is75PercentBadgeUnlocked));
            OnPropertyChanged(nameof(Is100PercentBadgeUnlocked));

            UpdateQuote();
        }

        private void UpdateQuote()
        {
            double pct = ProgressPercentage;
            if (pct <= 0)
                MotivationalQuote = "Start your day fresh! Log your first glass of water.";
            else if (pct < 25)
                MotivationalQuote = "Good start! Every sip brings you closer to your goal.";
            else if (pct < 50)
                MotivationalQuote = "Keep it up! Hydration supports focus and energy.";
            else if (pct < 75)
                MotivationalQuote = "Over halfway there! Your body is thanking you.";
            else if (pct < 100)
                MotivationalQuote = "Almost at the finish line! Great job today.";
            else
                MotivationalQuote = "Goal achieved! Excellent hydration today! Keep it up.";
        }
    }
}
