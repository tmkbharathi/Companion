using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;
using tmkbCompanion.MVVM.ViewModel;

namespace tmkbCompanion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly TrayIconManager _trayIconManager;
        private bool _isShutdownSignaled;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            _trayIconManager = new TrayIconManager(this);

            // Wire tray events
            _trayIconManager.ToggleTimerRequested += TrayIconManager_ToggleTimerRequested;
            _trayIconManager.OpenRequested += TrayIconManager_OpenRequested;

            // Wire view model events
            _viewModel.DashboardVM.TimerStateChanged += DashboardVM_TimerStateChanged;
            _viewModel.DashboardVM.TimerFinished += DashboardVM_TimerFinished;
            _viewModel.SettingsVM.AccentColorChanged += SettingsVM_AccentColorChanged;

            // Set initial states
            _trayIconManager.UpdateTimerState(_viewModel.DashboardVM.IsTimerRunning);
            
            if (System.Windows.Application.Current.Resources["AccentColor"] is System.Windows.Media.Color accentColor)
            {
                _trayIconManager.UpdateIcon(accentColor);
            }

            // Ensure we handle Windows SessionEnding so we don't block OS shutdown/logout
            System.Windows.Application.Current.SessionEnding += (s, e) =>
            {
                _isShutdownSignaled = true;
            };
        }

        private void TrayIconManager_ToggleTimerRequested()
        {
            _viewModel.DashboardVM.ToggleTimer();
        }

        private void TrayIconManager_OpenRequested()
        {
            // Restoring window visual state is done inside TrayIconManager.OnOpenRequested(),
            // but we can ensure the window gets brought to front and show Dashboard.
            _viewModel.ShowDashboard();
        }

        private void DashboardVM_TimerStateChanged(bool isRunning)
        {
            _trayIconManager.UpdateTimerState(isRunning);
        }

        private void DashboardVM_TimerFinished()
        {
            // Show custom slide-in toast notification in-app
            _viewModel.ShowInAppToast("Focus Timer Finished!", "Great job! Time to take a break.");

            // Show native tray balloon notification if enabled in Settings
            if (_viewModel.SettingsVM.ShowNotifications)
            {
                _trayIconManager.ShowNotification("FlowTrack Focus Session", "Your focus session has finished! Time for a short break.");
            }
        }

        private void SettingsVM_AccentColorChanged(System.Windows.Media.Color color)
        {
            _trayIconManager.UpdateIcon(color);
        }

        public void Shutdown()
        {
            _isShutdownSignaled = true;
            this.Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isShutdownSignaled)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                _trayIconManager.Dispose();
                base.OnClosing(e);
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void MinimizeBtn_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            // Minimize to tray on close button click
            this.Hide();
        }
    }
}