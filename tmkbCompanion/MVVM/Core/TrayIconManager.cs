using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using Application = System.Windows.Application;

namespace tmkbCompanion.MVVM.Core
{
    public class TrayIconManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ToolStripMenuItem _toggleTimerMenuItem;
        private readonly ToolStripMenuItem _togglePetMenuItem;
        private readonly ToolStripMenuItem _linksMenuItem;
        private IntPtr _currentIconHandle = IntPtr.Zero;
        private readonly Window _mainWindow;

        public event Action? ToggleTimerRequested;
        public event Action? OpenRequested;
        public event Action<bool>? TogglePetRequested;
        public event Action? RunScriptRequested;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public TrayIconManager(Window mainWindow)
        {
            _mainWindow = mainWindow ?? throw new ArgumentNullException(nameof(mainWindow));

            // Create context menu
            var contextMenuStrip = new ContextMenuStrip();

            var openMenuItem = new ToolStripMenuItem("Open FlowTrack");
            openMenuItem.Click += (s, e) => OnOpenRequested();
            contextMenuStrip.Items.Add(openMenuItem);

            _toggleTimerMenuItem = new ToolStripMenuItem("Start Focus Timer");
            _toggleTimerMenuItem.Click += (s, e) => ToggleTimerRequested?.Invoke();
            contextMenuStrip.Items.Add(_toggleTimerMenuItem);

            _togglePetMenuItem = new ToolStripMenuItem("Pet Companion")
            {
                CheckOnClick = true,
                Checked = false
            };
            _togglePetMenuItem.Click += (s, e) => TogglePetRequested?.Invoke(_togglePetMenuItem.Checked);
            contextMenuStrip.Items.Add(_togglePetMenuItem);

            _linksMenuItem = new ToolStripMenuItem("Important Links");
            contextMenuStrip.Items.Add(_linksMenuItem);

            var runScriptMenuItem = new ToolStripMenuItem("Run Custom Script");
            runScriptMenuItem.Click += (s, e) => RunScriptRequested?.Invoke();
            contextMenuStrip.Items.Add(runScriptMenuItem);

            contextMenuStrip.Items.Add(new ToolStripSeparator());

            var exitMenuItem = new ToolStripMenuItem("Exit");
            exitMenuItem.Click += (s, e) => {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                }
                if (_mainWindow is MainWindow mainWin)
                {
                    mainWin.Shutdown();
                }
                else
                {
                    Application.Current.Shutdown();
                }
            };
            contextMenuStrip.Items.Add(exitMenuItem);

            // Initialize NotifyIcon
            _notifyIcon = new NotifyIcon
            {
                ContextMenuStrip = contextMenuStrip,
                Text = "FlowTrack Productivity Companion",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => OnOpenRequested();

            // Set initial icon color (default Accent Blue: #5B8CFF)
            UpdateIcon(System.Windows.Media.Color.FromRgb(91, 140, 255));
        }

        public void UpdateIcon(System.Windows.Media.Color wpfColor)
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/Resources/Icons/favicon.ico");
                var streamInfo = Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    var newIcon = new Icon(streamInfo.Stream);
                    var oldIcon = _notifyIcon.Icon;
                    _notifyIcon.Icon = newIcon;
                    oldIcon?.Dispose();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
            }
        }

        public void UpdateTimerState(bool isRunning)
        {
            _toggleTimerMenuItem.Text = isRunning ? "Pause Focus Timer" : "Start Focus Timer";
        }

        public void UpdatePetState(bool isEnabled)
        {
            _togglePetMenuItem.Checked = isEnabled;
        }

        public void UpdateImportantLinks(List<(string Title, string Url)> links)
        {
            _linksMenuItem.DropDownItems.Clear();

            if (links == null || links.Count == 0)
            {
                var noLinksItem = new ToolStripMenuItem("No links configured") { Enabled = false };
                _linksMenuItem.DropDownItems.Add(noLinksItem);
                return;
            }

            foreach (var link in links)
            {
                if (string.IsNullOrWhiteSpace(link.Title) && string.IsNullOrWhiteSpace(link.Url))
                    continue;

                string displayTitle = string.IsNullOrWhiteSpace(link.Title) ? link.Url : link.Title;
                var menuItem = new ToolStripMenuItem(displayTitle);

                if (!string.IsNullOrWhiteSpace(link.Url))
                {
                    string targetUrl = link.Url;
                    menuItem.Click += (s, e) =>
                    {
                        try
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(targetUrl) { UseShellExecute = true });
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to open tray link: {ex.Message}");
                        }
                    };
                }
                else
                {
                    menuItem.Enabled = false;
                }

                _linksMenuItem.DropDownItems.Add(menuItem);
            }

            if (_linksMenuItem.DropDownItems.Count == 0)
            {
                var noLinksItem = new ToolStripMenuItem("No links configured") { Enabled = false };
                _linksMenuItem.DropDownItems.Add(noLinksItem);
            }
        }

        private void OnOpenRequested()
        {
            OpenRequested?.Invoke();
            
            // Restore window if minimized
            _mainWindow.Show();
            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }
            _mainWindow.Activate();
        }

        public void ShowContextMenu()
        {
            try
            {
                var screenPos = System.Windows.Forms.Cursor.Position;
                _notifyIcon.ContextMenuStrip?.Show(screenPos);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to show context menu: {ex.Message}");
            }
        }

        public void ShowNotification(string title, string message)
        {
            _notifyIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            if (_currentIconHandle != IntPtr.Zero)
            {
                DestroyIcon(_currentIconHandle);
                _currentIconHandle = IntPtr.Zero;
            }
        }
    }
}
