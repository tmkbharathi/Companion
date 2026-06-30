using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace tmkbCompanion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private const string MutexName = "Global\\FlowTrack_Productivity_Companion_Mutex_Unique_12345";
        private static Mutex? _mutex;

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern uint RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        protected override void OnStartup(StartupEventArgs e)
        {
            _mutex = new Mutex(true, MutexName, out bool isNewInstance);
            if (!isNewInstance)
            {
                uint msg = RegisterWindowMessage("FlowTrack_Show_Instance_Message");
                if (msg != 0)
                {
                    PostMessage((IntPtr)0xFFFF, msg, IntPtr.Zero, IntPtr.Zero);
                }

                // Shutdown this instance immediately
                System.Windows.Application.Current.Shutdown();
                return;
            }

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_mutex != null)
            {
                try
                {
                    _mutex.ReleaseMutex();
                }
                catch (ObjectDisposedException) { }
                catch (ApplicationException) { }
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}
