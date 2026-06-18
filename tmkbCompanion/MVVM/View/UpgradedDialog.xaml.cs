using System;
using System.Windows;
using System.Windows.Input;
using tmkbCompanion.MVVM.Core;

namespace tmkbCompanion.MVVM.View
{
    public partial class UpgradedDialog : Window
    {
        public UpgradedDialog(string oldVersion, string newVersion)
        {
            InitializeComponent();
            OldVersionText.Text = $"v{oldVersion}";
            NewVersionText.Text = $"v{newVersion}";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Register with PopupManager to close on ESC
            PopupManager.Push(this);
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}
