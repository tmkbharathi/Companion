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

        public static readonly DependencyProperty IsSuccessReactionActiveProperty =
            DependencyProperty.Register(nameof(IsSuccessReactionActive), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public bool IsSuccessReactionActive
        {
            get => (bool)GetValue(IsSuccessReactionActiveProperty);
            set => SetValue(IsSuccessReactionActiveProperty, value);
        }

        public static readonly DependencyProperty IsFailureReactionActiveProperty =
            DependencyProperty.Register(nameof(IsFailureReactionActive), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public bool IsFailureReactionActive
        {
            get => (bool)GetValue(IsFailureReactionActiveProperty);
            set => SetValue(IsFailureReactionActiveProperty, value);
        }

        public static readonly DependencyProperty IsDrinkReactionActiveProperty =
            DependencyProperty.Register(nameof(IsDrinkReactionActive), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false));

        public bool IsDrinkReactionActive
        {
            get => (bool)GetValue(IsDrinkReactionActiveProperty);
            set => SetValue(IsDrinkReactionActiveProperty, value);
        }

        public static readonly DependencyProperty IsWorkingActiveProperty =
            DependencyProperty.Register(nameof(IsWorkingActive), typeof(bool), typeof(PetCompanionView), new PropertyMetadata(false, OnIsWorkingActiveChanged));

        public bool IsWorkingActive
        {
            get => (bool)GetValue(IsWorkingActiveProperty);
            set => SetValue(IsWorkingActiveProperty, value);
        }

        private static void OnIsWorkingActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PetCompanionView view)
            {
                view.UpdateTypingState();
            }
        }

        private System.Windows.Threading.DispatcherTimer? _reactionTimer;
        private System.Windows.Threading.DispatcherTimer? _successReactionTimer;
        private System.Windows.Threading.DispatcherTimer? _failureReactionTimer;
        private System.Windows.Threading.DispatcherTimer? _drinkReactionTimer;
        private System.Windows.Threading.DispatcherTimer? _typingTimer;
        private System.Windows.Threading.DispatcherTimer? _drumrollTimer;
        private bool _drumrollLeftState;
        private bool _typingLeftState;

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

        public void TriggerSuccessReaction()
        {
            _successReactionTimer?.Stop();
            _drumrollTimer?.Stop();
            _reactionTimer?.Stop();

            IsClickedReactionActive = false;
            IsSuccessReactionActive = true;

            _drumrollLeftState = true;
            IsLeftPawPressed = true;
            IsRightPawPressed = false;

            _drumrollTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _drumrollTimer.Tick += (s, ev) =>
            {
                _drumrollLeftState = !_drumrollLeftState;
                IsLeftPawPressed = _drumrollLeftState;
                IsRightPawPressed = !_drumrollLeftState;
            };
            _drumrollTimer.Start();

            _successReactionTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _successReactionTimer.Tick += (s, ev) =>
            {
                IsSuccessReactionActive = false;
                IsLeftPawPressed = false;
                IsRightPawPressed = false;
                _drumrollTimer.Stop();
                _successReactionTimer.Stop();
            };
            _successReactionTimer.Start();
        }

        public void TriggerFailureReaction()
        {
            _successReactionTimer?.Stop();
            _drumrollTimer?.Stop();
            _reactionTimer?.Stop();
            _failureReactionTimer?.Stop();
            _drinkReactionTimer?.Stop();

            IsClickedReactionActive = false;
            IsSuccessReactionActive = false;
            IsFailureReactionActive = true;
            IsDrinkReactionActive = false;

            // Make paws go flat
            IsLeftPawPressed = false;
            IsRightPawPressed = false;

            _failureReactionTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(1500)
            };
            _failureReactionTimer.Tick += (s, ev) =>
            {
                IsFailureReactionActive = false;
                _failureReactionTimer.Stop();
            };
            _failureReactionTimer.Start();
        }

        public void TriggerDrinkWaterReaction()
        {
            _successReactionTimer?.Stop();
            _drumrollTimer?.Stop();
            _reactionTimer?.Stop();
            _failureReactionTimer?.Stop();
            _drinkReactionTimer?.Stop();

            IsClickedReactionActive = false;
            IsSuccessReactionActive = false;
            IsFailureReactionActive = false;
            IsDrinkReactionActive = true;

            IsLeftPawPressed = false;
            IsRightPawPressed = false;

            try
            {
                System.Media.SystemSounds.Asterisk.Play();
            }
            catch
            {
                // Fallback
            }

            _drinkReactionTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2200)
            };
            _drinkReactionTimer.Tick += (s, ev) =>
            {
                IsDrinkReactionActive = false;
                _drinkReactionTimer.Stop();
            };
            _drinkReactionTimer.Start();
        }

        private void UpdateTypingState()
        {
            if (IsWorkingActive)
            {
                if (_typingTimer == null)
                {
                    _typingTimer = new System.Windows.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromMilliseconds(400)
                    };
                    _typingTimer.Tick += TypingTimer_Tick;
                }
                _typingTimer.Start();
            }
            else
            {
                _typingTimer?.Stop();
                if (!IsClickedReactionActive && !IsSuccessReactionActive && !IsFailureReactionActive && !IsDrinkReactionActive)
                {
                    IsLeftPawPressed = false;
                    IsRightPawPressed = false;
                }
            }
        }

        private void TypingTimer_Tick(object? sender, EventArgs e)
        {
            if (IsClickedReactionActive || IsSuccessReactionActive || IsFailureReactionActive || IsDrinkReactionActive)
            {
                return;
            }

            _typingLeftState = !_typingLeftState;
            IsLeftPawPressed = _typingLeftState;
            IsRightPawPressed = !_typingLeftState;

            var releaseTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            releaseTimer.Tick += (s, ev) =>
            {
                if (!IsClickedReactionActive && !IsSuccessReactionActive && !IsFailureReactionActive && !IsDrinkReactionActive)
                {
                    IsLeftPawPressed = false;
                    IsRightPawPressed = false;
                }
                releaseTimer.Stop();
            };
            releaseTimer.Start();
        }

        public PetCompanionView()
        {
            InitializeComponent();
            this.Loaded += (s, e) => StartEyeTracking();
            this.Unloaded += (s, e) =>
            {
                StopEyeTracking();
                _reactionTimer?.Stop();
                _successReactionTimer?.Stop();
                _failureReactionTimer?.Stop();
                _drumrollTimer?.Stop();
                _drinkReactionTimer?.Stop();
                _typingTimer?.Stop();
            };
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
            if (IsClickedReactionActive || IsSuccessReactionActive || IsFailureReactionActive)
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
