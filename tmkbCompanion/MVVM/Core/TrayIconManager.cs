using System;
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
        private IntPtr _currentIconHandle = IntPtr.Zero;
        private readonly Window _mainWindow;

        public event Action? ToggleTimerRequested;
        public event Action? OpenRequested;

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
                var color = Color.FromArgb(wpfColor.A, wpfColor.R, wpfColor.G, wpfColor.B);
                
                using (var bitmap = new Bitmap(16, 16))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    
                    // Draw outer round ellipse
                    using (var brush = new SolidBrush(color))
                    {
                        g.FillEllipse(brush, 0, 0, 15, 15);
                    }

                    // Draw character 'F' in white inside the icon
                    using (var font = new System.Drawing.Font(new System.Drawing.FontFamily("Segoe UI"), 9, System.Drawing.FontStyle.Bold))
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString("F", font, brush, 3, 0);
                    }

                    IntPtr hIcon = bitmap.GetHicon();
                    var newIcon = Icon.FromHandle(hIcon);

                    _notifyIcon.Icon = newIcon;

                    // Clean up the previous icon handle to avoid memory leaks
                    if (_currentIconHandle != IntPtr.Zero)
                    {
                        DestroyIcon(_currentIconHandle);
                    }
                    _currentIconHandle = hIcon;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to generate tray icon: {ex.Message}");
            }
        }

        public void UpdateTimerState(bool isRunning)
        {
            _toggleTimerMenuItem.Text = isRunning ? "Pause Focus Timer" : "Start Focus Timer";
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
