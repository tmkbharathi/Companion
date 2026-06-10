using System;
using System.Threading.Tasks;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private object _currentView;
        private string _currentDate = string.Empty;
        private string _userName = "Alex Rivera";
        private string _toastTitle = string.Empty;
        private string _toastMessage = string.Empty;
        private bool _isToastVisible;
        private bool _isSidebarOpen = true;

        private readonly WaterReminderService _waterService;

        public DashboardViewModel DashboardVM { get; }
        public SettingsViewModel SettingsVM { get; }
        public ProfileSetupViewModel ProfileSetupVM { get; }
        public WaterReminderViewModel WaterReminderVM { get; }

        public object CurrentView
        {
            get => _currentView;
            set
            {
                if (SetProperty(ref _currentView, value))
                {
                    OnPropertyChanged(nameof(IsProfileTitleBarVisible));
                }
            }
        }

        public bool IsProfileTitleBarVisible => CurrentView != ProfileSetupVM;

        public string AppVersion => $"Utility v{System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.2"}";

        public string CurrentDate
        {
            get => _currentDate;
            set => SetProperty(ref _currentDate, value);
        }

        public string UserName
        {
            get => _userName;
            set
            {
                string safeValue = value ?? string.Empty;
                if (safeValue.Length > 20)
                {
                    safeValue = safeValue.Substring(0, 20);
                }
                SetProperty(ref _userName, safeValue);
            }
        }

        public string ToastTitle
        {
            get => _toastTitle;
            set => SetProperty(ref _toastTitle, value);
        }

        public string ToastMessage
        {
            get => _toastMessage;
            set => SetProperty(ref _toastMessage, value);
        }

        public bool IsToastVisible
        {
            get => _isToastVisible;
            set => SetProperty(ref _isToastVisible, value);
        }

        public ICommand ShowDashboardCommand { get; }
        public ICommand ShowSettingsCommand { get; }
        public ICommand ShowProfileSetupCommand { get; }
        public ICommand ShowWaterReminderCommand { get; }
        public ICommand ToggleSidebarCommand { get; }

        public bool IsSidebarOpen
        {
            get => _isSidebarOpen;
            set => SetProperty(ref _isSidebarOpen, value);
        }

        public MainViewModel()
        {
            // Initialize sub-view models
            _waterService = new WaterReminderService();
            DashboardVM = new DashboardViewModel();
            SettingsVM = new SettingsViewModel(DashboardVM);
            ProfileSetupVM = new ProfileSetupViewModel(this);
            WaterReminderVM = new WaterReminderViewModel(_waterService);

            // Set default view
            _currentView = DashboardVM;

            // Commands
            ShowDashboardCommand = new RelayCommand(ShowDashboard);
            ShowSettingsCommand = new RelayCommand(ShowSettings);
            ShowProfileSetupCommand = new RelayCommand(ShowProfileSetup);
            ShowWaterReminderCommand = new RelayCommand(ShowWaterReminder);
            ToggleSidebarCommand = new RelayCommand(() => IsSidebarOpen = !IsSidebarOpen);

            // Set current date string
            UpdateCurrentDate();
        }

        public void ShowDashboard()
        {
            CurrentView = DashboardVM;
        }

        public void ShowSettings()
        {
            CurrentView = SettingsVM;
        }

        public void ShowWaterReminder()
        {
            CurrentView = WaterReminderVM;
        }

        public void ShowProfileSetup()
        {
            CurrentView = ProfileSetupVM;
        }

        private void UpdateCurrentDate()
        {
            // E.g., "Wednesday, May 20, 2026"
            CurrentDate = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
        }

        public async void ShowInAppToast(string title, string message)
        {
            ToastTitle = title;
            ToastMessage = message;
            IsToastVisible = true;

            // Keep toast visible for 4 seconds, then fade out
            await Task.Delay(4000);
            IsToastVisible = false;
        }
    }
}
