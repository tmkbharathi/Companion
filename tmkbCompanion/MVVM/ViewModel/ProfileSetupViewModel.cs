using System;
using System.IO;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Win32;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.ViewModel
{
    public class ProfileSetupViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;
        private string _displayName = string.Empty;
        private string _bio = string.Empty;
        private bool _isDeepWorkSelected;
        private bool _isTasksSelected;
        private bool _isMeetingsSelected;
        private bool _isSmartNotificationsEnabled = true;
        private string _profilePicturePath = string.Empty;

        public string DisplayName
        {
            get => _displayName;
            set
            {
                if (value != null && value.Length > 20)
                {
                    value = value.Substring(0, 20);
                }
                SetProperty(ref _displayName, value);
            }
        }

        public string Bio
        {
            get => _bio;
            set => SetProperty(ref _bio, value);
        }

        public bool IsDeepWorkSelected
        {
            get => _isDeepWorkSelected;
            set => SetProperty(ref _isDeepWorkSelected, value);
        }

        public bool IsTasksSelected
        {
            get => _isTasksSelected;
            set => SetProperty(ref _isTasksSelected, value);
        }

        public bool IsMeetingsSelected
        {
            get => _isMeetingsSelected;
            set => SetProperty(ref _isMeetingsSelected, value);
        }

        public bool IsSmartNotificationsEnabled
        {
            get => _isSmartNotificationsEnabled;
            set => SetProperty(ref _isSmartNotificationsEnabled, value);
        }

        public string ProfilePicturePath
        {
            get => _profilePicturePath;
            set
            {
                if (SetProperty(ref _profilePicturePath, value))
                {
                    OnPropertyChanged(nameof(HasProfilePicture));
                }
            }
        }

        public bool HasProfilePicture => !string.IsNullOrEmpty(ProfilePicturePath) && File.Exists(ProfilePicturePath);

        public ICommand UploadPictureCommand { get; }
        public ICommand SaveProfileCommand { get; }
        public ICommand SkipCommand { get; }

        public ProfileSetupViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;

            UploadPictureCommand = new RelayCommand(UploadPicture);
            SaveProfileCommand = new RelayCommand(SaveProfile);
            SkipCommand = new RelayCommand(Skip);

            LoadProfileSettings();
        }

        private void UploadPicture()
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg|All files (*.*)|*.*",
                Title = "Select Profile Picture"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    var cropWindow = new tmkbCompanion.MVVM.View.CropImageWindow(openFileDialog.FileName)
                    {
                        Owner = System.Windows.Application.Current.MainWindow
                    };

                    if (cropWindow.ShowDialog() == true)
                    {
                        string finalImagePath = cropWindow.FinalImagePath;
                        if (!string.IsNullOrEmpty(finalImagePath) && File.Exists(finalImagePath))
                        {
                            ProfilePicturePath = finalImagePath;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _mainVM.ShowInAppToast("Error", $"Failed to process image: {ex.Message}");
                }
            }
        }

        private void SaveProfile()
        {
            try
            {
                var settings = new ProfileSettingsModel
                {
                    DisplayName = DisplayName,
                    Bio = Bio,
                    IsDeepWorkSelected = IsDeepWorkSelected,
                    IsTasksSelected = IsTasksSelected,
                    IsMeetingsSelected = IsMeetingsSelected,
                    IsSmartNotificationsEnabled = IsSmartNotificationsEnabled,
                    ProfilePicturePath = ProfilePicturePath
                };

                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profile_settings.json");
                string jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(settingsPath, jsonString);

                // Update main view model username
                if (!string.IsNullOrWhiteSpace(DisplayName))
                {
                    _mainVM.UserName = DisplayName;
                }

                // Show Toast Notification
                _mainVM.ShowInAppToast("Profile Saved", "Your profile preferences have been successfully updated.");

                // Redirect to Dashboard
                _mainVM.ShowDashboard();
            }
            catch (Exception ex)
            {
                _mainVM.ShowInAppToast("Error Saving Profile", ex.Message);
            }
        }

        private void Skip()
        {
            // Redirect to Dashboard
            _mainVM.ShowDashboard();
        }

        public void LoadProfileSettings()
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "profile_settings.json");
                if (File.Exists(settingsPath))
                {
                    string jsonString = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<ProfileSettingsModel>(jsonString);
                    if (settings != null)
                    {
                        DisplayName = settings.DisplayName;
                        Bio = settings.Bio;
                        IsDeepWorkSelected = settings.IsDeepWorkSelected;
                        IsTasksSelected = settings.IsTasksSelected;
                        IsMeetingsSelected = settings.IsMeetingsSelected;
                        IsSmartNotificationsEnabled = settings.IsSmartNotificationsEnabled;
                        ProfilePicturePath = settings.ProfilePicturePath;

                        if (!string.IsNullOrWhiteSpace(DisplayName))
                        {
                            _mainVM.UserName = DisplayName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load profile settings: {ex.Message}");
            }
        }
    }

    public class ProfileSettingsModel
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public bool IsDeepWorkSelected { get; set; }
        public bool IsTasksSelected { get; set; }
        public bool IsMeetingsSelected { get; set; }
        public bool IsSmartNotificationsEnabled { get; set; }
        public string ProfilePicturePath { get; set; } = string.Empty;
    }
}
