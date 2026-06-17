using System;
using System.Windows;
using System.Windows.Controls;

namespace tmkbCompanion.MVVM.Core
{
    public static class TextBlockService
    {
        public static readonly DependencyProperty ShowToolTipIfTrimmedProperty =
            DependencyProperty.RegisterAttached(
                "ShowToolTipIfTrimmed",
                typeof(bool),
                typeof(TextBlockService),
                new PropertyMetadata(false, OnShowToolTipIfTrimmedChanged));

        public static bool GetShowToolTipIfTrimmed(DependencyObject obj)
        {
            return (bool)obj.GetValue(ShowToolTipIfTrimmedProperty);
        }

        public static void SetShowToolTipIfTrimmed(DependencyObject obj, bool value)
        {
            obj.SetValue(ShowToolTipIfTrimmedProperty, value);
        }

        private static void OnShowToolTipIfTrimmedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TextBlock textBlock)
            {
                if ((bool)e.NewValue)
                {
                    textBlock.SizeChanged += TextBlock_SizeChanged;
                    textBlock.Loaded += TextBlock_Loaded;
                    var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                    dpd?.AddValueChanged(textBlock, TextBlock_TextChanged);
                }
                else
                {
                    textBlock.SizeChanged -= TextBlock_SizeChanged;
                    textBlock.Loaded -= TextBlock_Loaded;
                    var dpd = System.ComponentModel.DependencyPropertyDescriptor.FromProperty(TextBlock.TextProperty, typeof(TextBlock));
                    dpd?.RemoveValueChanged(textBlock, TextBlock_TextChanged);
                }
            }
        }

        private static void TextBlock_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateToolTip(sender as TextBlock);
        }

        private static void TextBlock_TextChanged(object? sender, EventArgs e)
        {
            UpdateToolTip(sender as TextBlock);
        }

        private static void TextBlock_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateToolTip(sender as TextBlock);
        }

        private static void UpdateToolTip(TextBlock? textBlock)
        {
            if (textBlock == null) return;
            textBlock.ToolTip = IsTextTrimmed(textBlock) ? textBlock.Text : null;
        }

        private static bool IsTextTrimmed(TextBlock textBlock)
        {
            if (textBlock.TextTrimming == TextTrimming.None)
                return false;

            textBlock.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            return textBlock.ActualWidth < textBlock.DesiredSize.Width;
        }
    }
}
