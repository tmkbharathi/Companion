using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private int _currentLinkPage = 0;
        private const int LinksPerPage = 4;

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

        // Dynamic Important Links Collections & Properties
        public ObservableCollection<ImportantLinkModel> ImportantLinks { get; } = new();
        public ObservableCollection<ImportantLinkModel> PagedLinks { get; } = new();

        public int CurrentLinkPage
        {
            get => _currentLinkPage;
            set
            {
                if (SetProperty(ref _currentLinkPage, value))
                {
                    UpdatePagedLinks();
                }
            }
        }

        public bool HasMultipleLinkPages => ImportantLinks.Count > LinksPerPage;

        // Commands
        public ICommand ToggleTimerCommand { get; }
        public ICommand ResetTimerCommand { get; }
        public ICommand SaveNotesCommand { get; }
        public ICommand OpenLinkCommand { get; }
        public ICommand AddLinkCommand { get; }
        public ICommand RemoveLinkCommand { get; }
        public ICommand PrevLinkPageCommand { get; }
        public ICommand NextLinkPageCommand { get; }

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
            AddLinkCommand = new RelayCommand(AddLink);
            RemoveLinkCommand = new RelayCommand(RemoveLink);
            PrevLinkPageCommand = new RelayCommand(PrevLinkPage, CanPrevLinkPage);
            NextLinkPageCommand = new RelayCommand(NextLinkPage, CanNextLinkPage);

            // Set up collections change listeners
            ImportantLinks.CollectionChanged += ImportantLinks_CollectionChanged;

            LoadNotes();
            LoadLinks();
        }

        private void ImportantLinks_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (ImportantLinkModel item in e.NewItems)
                {
                    item.PropertyChanged += Item_PropertyChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ImportantLinkModel item in e.OldItems)
                {
                    item.PropertyChanged -= Item_PropertyChanged;
                }
            }

            SaveLinks();
            UpdatePagedLinks();
            OnPropertyChanged(nameof(HasMultipleLinkPages));
            OnLinksChanged();
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SaveLinks();
            UpdatePagedLinks();
            OnLinksChanged();
        }

        public void UpdatePagedLinks()
        {
            PagedLinks.Clear();
            int start = CurrentLinkPage * LinksPerPage;
            for (int i = start; i < start + LinksPerPage && i < ImportantLinks.Count; i++)
            {
                PagedLinks.Add(ImportantLinks[i]);
            }

            (PrevLinkPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (NextLinkPageCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        private void AddLink()
        {
            ImportantLinks.Add(new ImportantLinkModel
            {
                Title = "New Link",
                Url = "https://example.com"
            });
            // Auto scroll to the new page if needed
            int maxPage = (ImportantLinks.Count - 1) / LinksPerPage;
            if (CurrentLinkPage < maxPage)
            {
                CurrentLinkPage = maxPage;
            }
        }

        private void RemoveLink(object? parameter)
        {
            if (parameter is ImportantLinkModel link)
            {
                ImportantLinks.Remove(link);
                int maxPage = (ImportantLinks.Count - 1) / LinksPerPage;
                if (CurrentLinkPage > maxPage && maxPage >= 0)
                {
                    CurrentLinkPage = maxPage;
                }
                else
                {
                    UpdatePagedLinks();
                }
            }
        }

        private void PrevLinkPage()
        {
            if (CurrentLinkPage > 0)
            {
                CurrentLinkPage--;
            }
        }

        private bool CanPrevLinkPage()
        {
            return CurrentLinkPage > 0;
        }

        private void NextLinkPage()
        {
            int maxPage = (ImportantLinks.Count - 1) / LinksPerPage;
            if (CurrentLinkPage < maxPage)
            {
                CurrentLinkPage++;
            }
        }

        private bool CanNextLinkPage()
        {
            int maxPage = (ImportantLinks.Count - 1) / LinksPerPage;
            return CurrentLinkPage < maxPage;
        }

        // Focus Timer Action Methods
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

        // Notes Storage Path Resolution
        public string NotesFilePath
        {
            get
            {
                try
                {
                    string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings.json");
                    if (File.Exists(settingsPath))
                    {
                        string json = File.ReadAllText(settingsPath);
                        using (var doc = System.Text.Json.JsonDocument.Parse(json))
                        {
                            if (doc.RootElement.TryGetProperty("QuickNotesPath", out var prop))
                            {
                                string customPath = prop.GetString() ?? string.Empty;
                                if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
                                {
                                    return Path.Combine(customPath, NotesFileName);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to resolve custom notes path: {ex.Message}");
                }
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, NotesFileName);
            }
        }

        public void ReloadNotes()
        {
            LoadNotes();
        }

        private void LoadNotes()
        {
            try
            {
                string path = NotesFilePath;
                if (File.Exists(path))
                {
                    NotesText = File.ReadAllText(path);
                }
                else
                {
                    NotesText = string.Empty;
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
                await File.WriteAllTextAsync(NotesFilePath, NotesText);
                
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

        // Links Load & Save Methods
        private void LoadLinks()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LinksFileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    
                    // 1. Try to load new dynamic list format
                    try
                    {
                        var data = System.Text.Json.JsonSerializer.Deserialize<LinkSettingsData>(json);
                        if (data != null && data.Links != null && data.Links.Count > 0)
                        {
                            ImportantLinks.Clear();
                            foreach (var link in data.Links)
                            {
                                ImportantLinks.Add(link);
                            }
                            UpdatePagedLinks();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to deserialize dynamic links list: {ex.Message}");
                    }

                    // 2. Try legacy fallback parsing
                    try
                    {
                        using (var doc = System.Text.Json.JsonDocument.Parse(json))
                        {
                            var root = doc.RootElement;
                            ImportantLinks.Clear();
                            if (root.TryGetProperty("Link1Title", out var t1) && root.TryGetProperty("Link1Url", out var u1))
                                ImportantLinks.Add(new ImportantLinkModel { Title = t1.GetString() ?? "", Url = u1.GetString() ?? "" });
                            if (root.TryGetProperty("Link2Title", out var t2) && root.TryGetProperty("Link2Url", out var u2))
                                ImportantLinks.Add(new ImportantLinkModel { Title = t2.GetString() ?? "", Url = u2.GetString() ?? "" });
                            if (root.TryGetProperty("Link3Title", out var t3) && root.TryGetProperty("Link3Url", out var u3))
                                ImportantLinks.Add(new ImportantLinkModel { Title = t3.GetString() ?? "", Url = u3.GetString() ?? "" });
                            if (root.TryGetProperty("Link4Title", out var t4) && root.TryGetProperty("Link4Url", out var u4))
                                ImportantLinks.Add(new ImportantLinkModel { Title = t4.GetString() ?? "", Url = u4.GetString() ?? "" });
                            
                            UpdatePagedLinks();
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Failed to parse legacy links: {ex.Message}");
                    }
                }

                // If file doesn't exist or is empty, seed defaults
                ImportantLinks.Clear();
                ImportantLinks.Add(new ImportantLinkModel { Title = "Company Portal", Url = "https://portal.company.com" });
                ImportantLinks.Add(new ImportantLinkModel { Title = "Documentation", Url = "https://docs.company.com" });
                ImportantLinks.Add(new ImportantLinkModel { Title = "Task Tracker", Url = "https://tasks.company.com" });
                ImportantLinks.Add(new ImportantLinkModel { Title = "Team Calendar", Url = "https://calendar.company.com" });
                UpdatePagedLinks();
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
                var data = new LinkSettingsData();
                foreach (var link in ImportantLinks)
                {
                    data.Links.Add(link);
                }
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
            foreach (var link in ImportantLinks)
            {
                if (!string.IsNullOrWhiteSpace(link.Title) || !string.IsNullOrWhiteSpace(link.Url))
                {
                    list.Add((link.Title, link.Url));
                }
            }
            return list;
        }
    }

    public class ImportantLinkModel : ViewModelBase
    {
        private string _title = string.Empty;
        private string _url = string.Empty;

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value ?? string.Empty);
        }

        public string Url
        {
            get => _url;
            set => SetProperty(ref _url, value ?? string.Empty);
        }
    }

    public class LinkSettingsData
    {
        public System.Collections.Generic.List<ImportantLinkModel> Links { get; set; } = new();
    }
}
