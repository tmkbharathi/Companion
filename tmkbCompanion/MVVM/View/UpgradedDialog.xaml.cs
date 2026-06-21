using System;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.View
{
    public partial class UpgradedDialog : System.Windows.Controls.UserControl
    {
        public event EventHandler? Closed;

        public UpgradedDialog(string oldVersion, string newVersion)
        {
            InitializeComponent();
            OldVersionText.Text = $"v{oldVersion}";
            NewVersionText.Text = $"v{newVersion}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // No registration with PopupManager as this is now a UserControl overlay.
        }

        public void Dismiss()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Dismiss();
        }
    }
}
