using System;
using System.Windows;
using System.Windows.Input;

namespace tmkbCompanion.MVVM.View
{
    public partial class PetCompanionWindow : System.Windows.Window
    {
        public bool IsLeftPawPressed
        {
            get => PetView.IsLeftPawPressed;
            set => PetView.IsLeftPawPressed = value;
        }

        public bool IsRightPawPressed
        {
            get => PetView.IsRightPawPressed;
            set => PetView.IsRightPawPressed = value;
        }

        private readonly Action? _showMenuAction;

        public PetCompanionWindow()
        {
            InitializeComponent();
        }

        public PetCompanionWindow(Action showMenuAction) : this()
        {
            _showMenuAction = showMenuAction;
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                PetView.TriggerClickReaction();

                try
                {
                    this.DragMove();
                }
                catch (Exception)
                {
                    // DragMove can throw an exception if the mouse click was released before the drag initiated.
                }
            }
        }

        private void Window_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Right)
            {
                _showMenuAction?.Invoke();
                e.Handled = true;
            }
        }
    }
}
