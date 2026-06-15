using System;
using System.Windows;
using System.Windows.Controls;

namespace tmkbCompanion.MVVM.View
{
    public partial class PetCompanionView : System.Windows.Controls.UserControl
    {
        public static readonly DependencyProperty IsLeftPawPressedProperty =
            DependencyProperty.Register(nameof(IsLeftPawPressed), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public static readonly DependencyProperty IsRightPawPressedProperty =
            DependencyProperty.Register(nameof(IsRightPawPressed), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public static readonly DependencyProperty IsClickedReactionActiveProperty =
            DependencyProperty.Register(nameof(IsClickedReactionActive), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public bool IsLeftPawPressed
        {
            get => (bool)GetValue(IsLeftPawPressedProperty);
            set => SetValue(IsLeftPawPressedProperty, value);
        }

        public bool IsRightPawPressed
        {
            get => (bool)GetValue(IsRightPawPressedProperty);
            set => SetValue(IsRightPawPressedProperty, value);
        }

        public bool IsClickedReactionActive
        {
            get => (bool)GetValue(IsClickedReactionActiveProperty);
            set => SetValue(IsClickedReactionActiveProperty, value);
        }

        private System.Windows.Threading.DispatcherTimer? _reactionTimer;

        public void TriggerClickReaction()
        {
            IsClickedReactionActive = true;

            if (_reactionTimer == null)
            {
                _reactionTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(1000)
                };
                _reactionTimer.Tick += (s, ev) =>
                {
                    IsClickedReactionActive = false;
                    _reactionTimer.Stop();
                };
            }
            else
            {
                _reactionTimer.Stop();
            }
            _reactionTimer.Start();
        }

        public PetCompanionView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => StartEyeTracking();
            this.Unloaded += (s, e) => StopEyeTracking();
        }

        private System.Windows.Threading.DispatcherTimer? _eyeTrackingTimer;

        private void StartEyeTracking()
        {
            if (_eyeTrackingTimer == null)
            {
                _eyeTrackingTimer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(30)
                };
                _eyeTrackingTimer.Tick += EyeTrackingTimer_Tick;
            }
            _eyeTrackingTimer.Start();
        }

        private void StopEyeTracking()
        {
            _eyeTrackingTimer?.Stop();
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private void EyeTrackingTimer_Tick(object? sender, EventArgs e)
        {
            if (IsClickedReactionActive)
            {
                if (LeftEyeTransform != null)
                {
                    LeftEyeTransform.X = 0;
                    LeftEyeTransform.Y = 0;
                }
                if (RightEyeTransform != null)
                {
                    RightEyeTransform.X = 0;
                    RightEyeTransform.Y = 0;
                }
                return;
            }

            if (GetCursorPos(out POINT screenPt))
            {
                try
                {
                    System.Windows.Point localPt = this.PointFromScreen(new System.Windows.Point(screenPt.X, screenPt.Y));
                    
                    if (LeftEyeTransform != null)
                    {
                        UpdateEyeOffset(localPt, 45, 48, LeftEyeTransform);
                    }

                    if (RightEyeTransform != null)
                    {
                        UpdateEyeOffset(localPt, 75, 48, RightEyeTransform);
                    }
                }
                catch
                {
                    // Handle case where visual is not connected to a presentation source yet/anymore
                }
            }
        }

        private void UpdateEyeOffset(System.Windows.Point mouseLocalPt, double centerX, double centerY, System.Windows.Media.TranslateTransform transform)
        {
            double dx = mouseLocalPt.X - centerX;
            double dy = mouseLocalPt.Y - centerY;
            double distance = Math.Sqrt(dx * dx + dy * dy);

            const double maxOffset = 2.5;

            if (distance == 0)
            {
                transform.X = 0;
                transform.Y = 0;
            }
            else
            {
                transform.X = (dx / distance) * Math.Min(distance, maxOffset);
                transform.Y = (dy / distance) * Math.Min(distance, maxOffset);
            }
        }
    }
}
