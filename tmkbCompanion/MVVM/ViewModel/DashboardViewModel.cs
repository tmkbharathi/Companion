using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class DashboardViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _timer;
        private int _timerSeconds = 1500; // 25 minutes
        private bool _isTimerRunning;
        private string _timerText = "25:00";
        private string _notesText = string.Empty;
        private string _saveButtonText = "Save Note";
        private bool _isSavedFeedbackActive;

        private const string NotesFileName = "flowtrack_notes.txt";

        public event Action<bool>? TimerStateChanged;
        public event Action? TimerFinished;

        public string TimerText
        {
            get => _timerText;
            set => SetProperty(ref _timerText, value);
        }

        public bool IsTimerRunning
        {
            get => _isTimerRunning;
            set
            {
                if (SetProperty(ref _isTimerRunning, value))
                {
                    TimerStateChanged?.Invoke(value);
                }
            }
        }

        public string NotesText
        {
            get => _notesText;
            set => SetProperty(ref _notesText, value);
        }

        public string SaveButtonText
        {
            get => _saveButtonText;
            set => SetProperty(ref _saveButtonText, value);
        }

        public bool IsSavedFeedbackActive
        {
            get => _isSavedFeedbackActive;
            set => SetProperty(ref _isSavedFeedbackActive, value);
        }

        public ICommand ToggleTimerCommand { get; }
        public ICommand ResetTimerCommand { get; }
        public ICommand SaveNotesCommand { get; }
        public ICommand OpenLinkCommand { get; }

        public DashboardViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += Timer_Tick;

            ToggleTimerCommand = new RelayCommand(ToggleTimer);
            ResetTimerCommand = new RelayCommand(ResetTimer);
            SaveNotesCommand = new RelayCommand(SaveNotes);
            OpenLinkCommand = new RelayCommand(OpenLink);

            LoadNotes();
        }

        public void ToggleTimer()
        {
            if (IsTimerRunning)
            {
                _timer.Stop();
                IsTimerRunning = false;
            }
            else
            {
                _timer.Start();
                IsTimerRunning = true;
            }
        }

        public void ResetTimer()
        {
            _timer.Stop();
            IsTimerRunning = false;
            _timerSeconds = 1500;
            UpdateTimerText();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            if (_timerSeconds > 0)
            {
                _timerSeconds--;
                UpdateTimerText();
            }
            else
            {
                _timer.Stop();
                IsTimerRunning = false;
                _timerSeconds = 1500;
                UpdateTimerText();
                TimerFinished?.Invoke();
            }
        }

        private void UpdateTimerText()
        {
            int mins = _timerSeconds / 60;
            int secs = _timerSeconds % 60;
            TimerText = $"{mins:D2}:{secs:D2}";
        }

        private void LoadNotes()
        {
            try
            {
                if (File.Exists(NotesFileName))
                {
                    NotesText = File.ReadAllText(NotesFileName);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load notes: {ex.Message}");
            }
        }

        private async void SaveNotes()
        {
            try
            {
                await File.WriteAllTextAsync(NotesFileName, NotesText);
                
                // Show success feedback
                SaveButtonText = "Saved";
                IsSavedFeedbackActive = true;

                await Task.Delay(2000);

                SaveButtonText = "Save Note";
                IsSavedFeedbackActive = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save notes: {ex.Message}");
            }
        }

        private void OpenLink(object? parameter)
        {
            if (parameter is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to open link: {ex.Message}");
                }
            }
        }
    }
}
