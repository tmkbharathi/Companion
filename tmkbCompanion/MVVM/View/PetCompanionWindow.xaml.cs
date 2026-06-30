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

        public bool IsWorkingActive
        {
            get => PetView.IsWorkingActive;
            set => PetView.IsWorkingActive = value;
        }

        private readonly Action? _showMenuAction;

        public PetCompanionWindow()
        {
            InitializeComponent();

            // Set initial position to bottom-right corner of the primary screen's work area
            this.Loaded += (s, e) =>
            {
                var workArea = SystemParameters.WorkArea;
                this.Left = workArea.Left + workArea.Width - this.Width - 20;
                this.Top = workArea.Top + workArea.Height - this.Height - 20;
            };
        }

        public PetCompanionWindow(Action showMenuAction) : this()
        {
            _showMenuAction = showMenuAction;
        }

        public void TriggerSuccessReaction()
        {
            PetView.TriggerSuccessReaction();
        }

        public void TriggerFailureReaction()
        {
            PetView.TriggerFailureReaction();
        }

        public void TriggerDrinkWaterReaction()
        {
            PetView.TriggerDrinkWaterReaction();
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
