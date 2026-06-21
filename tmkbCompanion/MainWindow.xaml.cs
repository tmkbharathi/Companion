using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using tmkbCompanion.MVVM.Core;
using tmkbCompanion.MVVM.ViewModel;
using tmkbCompanion.MVVM.View;

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
            _trayIconManager.TogglePetRequested += (isEnabled) =>
            {
                _viewModel.SettingsVM.IsPetEnabled = isEnabled;
            };
            _trayIconManager.RunScriptRequested += TrayIconManager_RunScriptRequested;

            // Wire view model events
            _viewModel.DashboardVM.TimerStateChanged += DashboardVM_TimerStateChanged;
            _viewModel.DashboardVM.TimerFinished += DashboardVM_TimerFinished;
            _viewModel.SettingsVM.AccentColorChanged += SettingsVM_AccentColorChanged;
            _viewModel.DashboardVM.LinksChanged += DashboardVM_LinksChanged;
            _viewModel.WaterReminderVM.Service.FallbackNotificationRequested += (title, msg) =>
            {
                _trayIconManager.ShowNotification(title, msg);
            };
            _viewModel.WaterReminderVM.Service.DrinkLogged += WaterReminderService_DrinkLogged;
            _viewModel.RunScriptVM.ScriptExecutedSuccessfully += RunScriptVM_ScriptExecutedSuccessfully;
            _viewModel.RunScriptVM.ScriptExecutionFailed += RunScriptVM_ScriptExecutionFailed;

            // Set initial states
            _trayIconManager.UpdateTimerState(_viewModel.DashboardVM.IsTimerRunning);
            _trayIconManager.UpdatePetState(_viewModel.SettingsVM.IsPetEnabled);
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

            // Watch settings toggle to manage pet window
            _viewModel.SettingsVM.PropertyChanged += SettingsVM_PropertyChanged;
            
            // Defer initial visibility check until the window is loaded to prevent setting Owner before showing
            this.Loaded += async (s, e) =>
            {
                UpdatePetWindowVisibility();
                await CheckForUpgradeGreetingAsync();

                // Run silent update check after greeting has finished and if not skipped
                if (!_viewModel.SettingsVM.DoNotShowUpdateAgain)
                {
                    _viewModel.UpdateService.CheckForUpdates(isManualCheck: false);
                }
            };
        }

        // ── Sidebar animation ──────────────────────────────────────────────

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.IsSidebarOpen))
            {
                double currentWidth = SidebarColumn.Width.Value;
                double targetWidth = _viewModel.IsSidebarOpen ? 160.0 : 0.0;
                AnimateSidebarWidth(currentWidth, targetWidth, milliseconds: 200);
            }
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
            if (_viewModel.SettingsVM.IsPetEnabled)
            {
                _petWindow?.Show();
            }
        }

        private async void TrayIconManager_RunScriptRequested()
        {
            if (_viewModel.RunScriptVM.IsRunning)
            {
                _trayIconManager.ShowNotification("Script Runner", "A custom script is already running.");
                return;
            }

            string script = _viewModel.RunScriptVM.ScriptContent;
            if (string.IsNullOrWhiteSpace(script))
            {
                _trayIconManager.ShowNotification("Script Runner", "No custom script is configured. Open the app to set one up.");
                return;
            }

            _trayIconManager.ShowNotification("Script Runner", $"Running script using {_viewModel.RunScriptVM.TerminalType}...");
            
            try
            {
                await _viewModel.RunScriptVM.RunScriptAsync();
                string status = _viewModel.RunScriptVM.StatusText;
                _trayIconManager.ShowNotification("Script Runner", $"Execution finished: {status}");
            }
            catch (Exception ex)
            {
                _trayIconManager.ShowNotification("Script Runner", $"Failed to execute script: {ex.Message}");
            }
        }

        private void RunScriptVM_ScriptExecutedSuccessfully(object? sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.SettingsVM.IsPetEnabled && _petWindow != null)
                {
                    _petWindow.TriggerSuccessReaction();
                }
            }));
        }

        private void RunScriptVM_ScriptExecutionFailed(object? sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.SettingsVM.IsPetEnabled && _petWindow != null)
                {
                    _petWindow.TriggerFailureReaction();
                }
            }));
        }

        private void WaterReminderService_DrinkLogged(int amount)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.SettingsVM.IsPetEnabled && _petWindow != null)
                {
                    _petWindow.TriggerDrinkWaterReaction();
                }
            }));
        }

        private void DashboardVM_TimerStateChanged(bool isRunning)
        {
            _trayIconManager.UpdateTimerState(isRunning);
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_petWindow != null)
                {
                    _petWindow.IsWorkingActive = isRunning;
                }
            }));
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
                StopGlobalKeyboardHook();

                // Remove clipboard format listener
                var windowClipboardSource = PresentationSource.FromVisual(this) as HwndSource;
                if (windowClipboardSource != null)
                {
                    RemoveClipboardFormatListener(windowClipboardSource.Handle);
                    windowClipboardSource.RemoveHook(ClipboardWndProc);
                }

                _viewModel.RunScriptVM.ScriptExecutedSuccessfully -= RunScriptVM_ScriptExecutedSuccessfully;
                _viewModel.RunScriptVM.ScriptExecutionFailed -= RunScriptVM_ScriptExecutionFailed;
                _viewModel.WaterReminderVM.Service.DrinkLogged -= WaterReminderService_DrinkLogged;
                _petWindow?.Close();
                _trayIconManager.Dispose();
                _viewModel.WaterReminderVM.Service.Dispose();
                base.OnClosing(e);
            }
        }

        private void SettingsVM_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SettingsViewModel.IsPetEnabled))
            {
                UpdatePetWindowVisibility();
                _trayIconManager.UpdatePetState(_viewModel.SettingsVM.IsPetEnabled);
            }
        }

        private void UpdatePetWindowVisibility()
        {
            if (_viewModel.SettingsVM.IsPetEnabled)
            {
                if (_petWindow == null)
                {
                    _petWindow = new PetCompanionWindow(() => _trayIconManager.ShowContextMenu());
                    _petWindow.IsWorkingActive = _viewModel.DashboardVM.IsTimerRunning;
                    _petWindow.Show();
                }
                StartGlobalKeyboardHook();
            }
            else
            {
                StopGlobalKeyboardHook();
                if (_petWindow != null)
                {
                    _petWindow.Close();
                    _petWindow = null;
                }
            }
        }

        private PetCompanionWindow? _petWindow;
        private GlobalKeyboardHook? _globalHook;
        private bool _lastPressedWasLeft = false;
        private System.Windows.Threading.DispatcherTimer? _pawReleaseTimer;

        private void StartGlobalKeyboardHook()
        {
            if (_globalHook == null)
            {
                try
                {
                    _globalHook = new GlobalKeyboardHook();
                    _globalHook.KeyDown += GlobalHook_KeyDown;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to start global keyboard hook: {ex.Message}");
                }
            }
        }

        private void StopGlobalKeyboardHook()
        {
            if (_globalHook != null)
            {
                _globalHook.KeyDown -= GlobalHook_KeyDown;
                _globalHook.Dispose();
                _globalHook = null;
            }
        }

        private void GlobalHook_KeyDown()
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (_viewModel.SettingsVM.IsPetEnabled && _petWindow != null)
                {
                    TriggerPawPress();
                }
            }));
        }

        private void TriggerPawPress()
        {
            if (_petWindow == null) return;

            if (_lastPressedWasLeft)
            {
                _petWindow.IsRightPawPressed = true;
                _petWindow.IsLeftPawPressed = false;
                _lastPressedWasLeft = false;
            }
            else
            {
                _petWindow.IsLeftPawPressed = true;
                _petWindow.IsRightPawPressed = false;
                _lastPressedWasLeft = true;
            }

            if (_pawReleaseTimer == null)
            {
                _pawReleaseTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(120)
                };
                _pawReleaseTimer.Tick += (s, ev) =>
                {
                    if (_petWindow != null)
                    {
                        _petWindow.IsLeftPawPressed = false;
                        _petWindow.IsRightPawPressed = false;
                    }
                    _pawReleaseTimer.Stop();
                };
            }
            else
            {
                _pawReleaseTimer.Stop();
            }
            _pawReleaseTimer.Start();
        }

        /// <summary>
        /// Route ESC key to the popup manager so the topmost open popup is
        /// always closed first. All future popups benefit automatically.
        /// </summary>
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                if (UpgradeGreetingOverlay.Visibility == Visibility.Visible)
                {
                    if (UpgradeGreetingContent.Content is UpgradedDialog dialog)
                    {
                        dialog.Dismiss();
                        e.Handled = true;
                        return;
                    }
                }
                if (PopupManager.HasOpenPopups)
                {
                    PopupManager.CloseTop();
                    e.Handled = true;
                    return;
                }
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

        // ── Win32 Clipboard Listener ────────────────────────────────────────

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        private const int WM_CLIPBOARDUPDATE = 0x031D;

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var windowClipboardSource = PresentationSource.FromVisual(this) as HwndSource;
            if (windowClipboardSource != null)
            {
                windowClipboardSource.AddHook(ClipboardWndProc);
                AddClipboardFormatListener(windowClipboardSource.Handle);
            }
        }

        private IntPtr ClipboardWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_CLIPBOARDUPDATE)
            {
                try
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        string text = System.Windows.Clipboard.GetText();
                        _viewModel.ClipboardHistoryVM.AddClipboardItem(text);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to retrieve clipboard text: {ex.Message}");
                }
            }
            return IntPtr.Zero;
        }

        private async System.Threading.Tasks.Task CheckForUpgradeGreetingAsync()
        {
            try
            {
                string currentVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.1.2";
                string lastVersion = _viewModel.SettingsVM.LastLaunchedVersion;

                // Only show greeting if the app has run before (lastVersion is not empty) and it is different from the current version (meaning we just updated)
                if (!string.IsNullOrEmpty(lastVersion) && lastVersion != currentVersion)
                {
                    var tcs = new System.Threading.Tasks.TaskCompletionSource<bool>();

                    var greetingView = new UpgradedDialog(lastVersion, currentVersion);
                    greetingView.Closed += (s, e) =>
                    {
                        UpgradeGreetingOverlay.Visibility = Visibility.Collapsed;
                        UpgradeGreetingContent.Content = null;
                        tcs.SetResult(true);
                    };

                    UpgradeGreetingContent.Content = greetingView;
                    UpgradeGreetingOverlay.Visibility = Visibility.Visible;

                    // Wait for the overlay to close
                    await tcs.Task;
                }

                // Update the last launched version in settings so it doesn't show again until the next upgrade
                if (lastVersion != currentVersion)
                {
                    _viewModel.SettingsVM.LastLaunchedVersion = currentVersion;
                    _viewModel.SettingsVM.DoNotShowUpdateAgain = false;
                    _viewModel.SettingsVM.SaveSettings();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to check for upgrade greeting: {ex.Message}");
            }
        }
    }

    internal class GlobalKeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private readonly LowLevelKeyboardProc _proc;
        private IntPtr _hookId = IntPtr.Zero;

        public event Action? KeyDown;

        public GlobalKeyboardHook()
        {
            _proc = HookCallback;
            _hookId = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                string? moduleName = curModule?.ModuleName;
                IntPtr hModule = IntPtr.Zero;
                if (!string.IsNullOrEmpty(moduleName))
                {
                    hModule = GetModuleHandle(moduleName);
                }
                
                if (hModule == IntPtr.Zero)
                {
                    hModule = GetModuleHandle(null);
                }

                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, hModule, 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
            {
                KeyDown?.Invoke();
            }
            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            if (_hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookId);
                _hookId = IntPtr.Zero;
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);
    }
}