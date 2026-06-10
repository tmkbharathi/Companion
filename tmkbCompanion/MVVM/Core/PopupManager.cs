using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;

namespace tmkbCompanion.MVVM.Core
{
    /// <summary>
    /// Central popup manager. Maintains a stack of open popup windows so that
    /// pressing ESC always closes the most-recently-opened popup first.
    ///
    /// Usage:
    ///   1. In the popup window's Loaded event call <c>PopupManager.Push(this)</c>.
    ///   2. The stack auto-pops when the window raises its <c>Closed</c> event.
    ///   3. MainWindow's KeyDown handler calls <c>PopupManager.CloseTop()</c> on ESC.
    /// </summary>
    public static class PopupManager
    {
        private static readonly List<Window> _openWindows = new List<Window>();

        /// <summary>Returns true when at least one popup is registered.</summary>
        public static bool HasOpenPopups => _openWindows.Count > 0;

        /// <summary>
        /// Push a window onto the collection. Automatically removes it when it closes.
        /// Must be called from the UI thread (e.g. inside a Loaded event handler).
        /// </summary>
        public static void Push(Window window)
        {
            if (window == null) return;

            _openWindows.Add(window);

            window.Closed += (sender, args) =>
            {
                _openWindows.Remove(window);
            };
        }

        /// <summary>
        /// Close the topmost registered popup window.
        /// Must be called from the UI thread.
        /// </summary>
        public static void CloseTop()
        {
            if (_openWindows.Count == 0) return;

            var top = _openWindows[_openWindows.Count - 1];

            // Use dispatcher to ensure we're on the right thread.
            top.Dispatcher.Invoke(() => top.Close(), DispatcherPriority.Normal);
        }
    }
}
