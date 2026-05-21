using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
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
            _viewModel.DashboardVM.LinksChanged += DashboardVM_LinksChanged;

            // Set initial states
            _trayIconManager.UpdateTimerState(_viewModel.DashboardVM.IsTimerRunning);
            UpdateTrayLinks();
            
            if (System.Windows.Application.Current.Resources["AccentColor"] is System.Windows.Media.Color accentColor)
            {
                _trayIconManager.UpdateIcon(accentColor);
            }

            // Ensure we handle Windows SessionEnding so we don't block OS shutdown/logout
            System.Windows.Application.Current.SessionEnding += (s, e) =>
            {
                _isShutdownSignaled = true;
            };

            // Watch sidebar toggle → animate column width
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        // ── Sidebar animation ──────────────────────────────────────────────

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSidebarOpen))
                AnimateSidebarWidth(_viewModel.IsSidebarOpen ? 0.0 : 160.0,
                                    _viewModel.IsSidebarOpen ? 160.0 : 0.0,
                                    milliseconds: 200);
        }

        private System.Windows.Threading.DispatcherTimer? _sidebarTimer;
        private double _sbFrom;
        private double _sbTo;
        private double _sbElapsed;

        /// <summary>
        /// Animates SidebarColumn.Width using a DispatcherTimer + cubic ease-in-out.
        /// WPF cannot animate GridLength directly, so we drive it manually each tick.
        /// </summary>
        private void AnimateSidebarWidth(double from, double to, double milliseconds)
        {
            _sidebarTimer?.Stop();
            _sbFrom    = from;
            _sbTo      = to;
            _sbElapsed = 0;

            _sidebarTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(8) // ~120 fps
            };

            _sidebarTimer.Tick += (s, e) =>
            {
                _sbElapsed += 8;
                double t = Math.Min(_sbElapsed / milliseconds, 1.0);

                // Cubic ease-in-out
                double eased = t < 0.5
                    ? 4 * t * t * t
                    : 1 - Math.Pow(-2 * t + 2, 3) / 2;

                SidebarColumn.Width = new GridLength(_sbFrom + (_sbTo - _sbFrom) * eased);

                if (t >= 1.0)
                {
                    SidebarColumn.Width = new GridLength(_sbTo);
                    _sidebarTimer!.Stop();
                }
            };

            _sidebarTimer.Start();
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

        private void DashboardVM_LinksChanged()
        {
            UpdateTrayLinks();
        }

        private void UpdateTrayLinks()
        {
            var links = _viewModel.DashboardVM.GetImportantLinks();
            _trayIconManager.UpdateImportantLinks(links);
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

        /// <summary>
        /// Route ESC key to the popup manager so the topmost open popup is
        /// always closed first. All future popups benefit automatically.
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape && PopupManager.HasOpenPopups)
            {
                PopupManager.CloseTop();
                e.Handled = true;
                return;
            }
            base.OnKeyDown(e);
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