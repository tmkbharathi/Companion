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

        private string _link1Title = "Company Portal";
        private string _link1Url = "https://portal.company.com";
        private string _link2Title = "Documentation";
        private string _link2Url = "https://docs.company.com";
        private string _link3Title = "Task Tracker";
        private string _link3Url = "https://tasks.company.com";
        private string _link4Title = "Team Calendar";
        private string _link4Url = "https://calendar.company.com";

        private const string NotesFileName = "flowtrack_notes.txt";
        private const string LinksFileName = "link_settings.json";

        public event Action<bool>? TimerStateChanged;
        public event Action? TimerFinished;
        public event Action? LinksChanged;

        private void OnLinksChanged()
        {
            LinksChanged?.Invoke();
        }

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

        public string Link1Title { get => _link1Title; set { if (SetProperty(ref _link1Title, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link1Url { get => _link1Url; set { if (SetProperty(ref _link1Url, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link2Title { get => _link2Title; set { if (SetProperty(ref _link2Title, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link2Url { get => _link2Url; set { if (SetProperty(ref _link2Url, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link3Title { get => _link3Title; set { if (SetProperty(ref _link3Title, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link3Url { get => _link3Url; set { if (SetProperty(ref _link3Url, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link4Title { get => _link4Title; set { if (SetProperty(ref _link4Title, value)) { SaveLinks(); OnLinksChanged(); } } }
        public string Link4Url { get => _link4Url; set { if (SetProperty(ref _link4Url, value)) { SaveLinks(); OnLinksChanged(); } } }

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
            LoadLinks();
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

        private void LoadLinks()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LinksFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var data = System.Text.Json.JsonSerializer.Deserialize<LinkSettingsData>(json);
                    if (data != null)
                    {
                        _link1Title = data.Link1Title;
                        _link1Url = data.Link1Url;
                        _link2Title = data.Link2Title;
                        _link2Url = data.Link2Url;
                        _link3Title = data.Link3Title;
                        _link3Url = data.Link3Url;
                        _link4Title = data.Link4Title;
                        _link4Url = data.Link4Url;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load links: {ex.Message}");
            }
        }

        private void SaveLinks()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LinksFileName);
                var data = new LinkSettingsData
                {
                    Link1Title = Link1Title,
                    Link1Url = Link1Url,
                    Link2Title = Link2Title,
                    Link2Url = Link2Url,
                    Link3Title = Link3Title,
                    Link3Url = Link3Url,
                    Link4Title = Link4Title,
                    Link4Url = Link4Url
                };
                string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save links: {ex.Message}");
            }
        }

        public System.Collections.Generic.List<(string Title, string Url)> GetImportantLinks()
        {
            var list = new System.Collections.Generic.List<(string Title, string Url)>();
            if (!string.IsNullOrWhiteSpace(Link1Title) || !string.IsNullOrWhiteSpace(Link1Url))
                list.Add((Link1Title, Link1Url));
            if (!string.IsNullOrWhiteSpace(Link2Title) || !string.IsNullOrWhiteSpace(Link2Url))
                list.Add((Link2Title, Link2Url));
            if (!string.IsNullOrWhiteSpace(Link3Title) || !string.IsNullOrWhiteSpace(Link3Url))
                list.Add((Link3Title, Link3Url));
            if (!string.IsNullOrWhiteSpace(Link4Title) || !string.IsNullOrWhiteSpace(Link4Url))
                list.Add((Link4Title, Link4Url));
            return list;
        }
    }

    public class LinkSettingsData
    {
        public string Link1Title { get; set; } = "Company Portal";
        public string Link1Url { get; set; } = "https://portal.company.com";
        public string Link2Title { get; set; } = "Documentation";
        public string Link2Url { get; set; } = "https://docs.company.com";
        public string Link3Title { get; set; } = "Task Tracker";
        public string Link3Url { get; set; } = "https://tasks.company.com";
        public string Link4Title { get; set; } = "Team Calendar";
        public string Link4Url { get; set; } = "https://calendar.company.com";
    }
}
