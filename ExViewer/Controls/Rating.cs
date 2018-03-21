﻿using ExClient.Galleries.Rating;
using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

// The Templated Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234235

namespace ExViewer.Controls
{
    public sealed class Rating : Control
    {
        public Rating()
        {
            this.DefaultStyleKey = typeof(Rating);
        }

        public double PlaceholderValue
        {
            get => (double)GetValue(PlaceholderValueProperty);
            set => SetValue(PlaceholderValueProperty, value);
        }

        /// <summary>
        /// Indentify <see cref="PlaceholderValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlaceholderValueProperty =
            DependencyProperty.Register(nameof(PlaceholderValue), typeof(double), typeof(Rating), new PropertyMetadata(0d, PlaceholderValuePropertyChanged));

        private static void PlaceholderValuePropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (double)e.OldValue;
            var newValue = (double)e.NewValue;
            if (oldValue == newValue)
                return;
            var sender = (Rating)dp;
            if (double.IsNaN(newValue) || newValue > 5 || newValue < 0)
                throw new ArgumentOutOfRangeException(nameof(PlaceholderValue));
            sender.draw();
        }

        public Score UserRatingValue
        {
            get => (Score)GetValue(UserRatingValueProperty);
            set => SetValue(UserRatingValueProperty, value);
        }

        /// <summary>
        /// Indentify <see cref="UserRatingValue"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty UserRatingValueProperty =
            DependencyProperty.Register(nameof(UserRatingValue), typeof(Score), typeof(Rating), new PropertyMetadata(Score.NotSet, UserRatingValuePropertyChanged));

        private static void UserRatingValuePropertyChanged(DependencyObject dp, DependencyPropertyChangedEventArgs e)
        {
            var oldValue = (Score)e.OldValue;
            var newValue = (Score)e.NewValue;
            if (oldValue == newValue)
                return;
            var sender = (Rating)dp;
            sender.actualUserRating = newValue;
            sender.draw();
        }

        private TextBlock tbBackground;
        private TextBlock tbPlaceholder;
        private TextBlock tbUserRating;
        private RectangleGeometry rgPlaceholderClip;
        private RectangleGeometry rgUserRatingClip;

        private Score actualUserRating = Score.NotSet;

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            base.OnLostFocus(e);
            this.actualUserRating = this.UserRatingValue;
            draw();
        }

        protected override void OnKeyUp(KeyRoutedEventArgs e)
        {
            base.OnKeyUp(e);
            e.Handled = true;
            switch (e.OriginalKey)
            {
            case Windows.System.VirtualKey.Right:
            case Windows.System.VirtualKey.GamepadDPadRight:
            case Windows.System.VirtualKey.GamepadLeftThumbstickRight:
                if (this.actualUserRating == Score.NotSet)
                    this.actualUserRating = this.PlaceholderValue.ToScore();
                if (this.actualUserRating != Score.Score_5_0)
                {
                    this.actualUserRating++;
                }
                draw();
                break;
            case Windows.System.VirtualKey.Left:
            case Windows.System.VirtualKey.GamepadDPadLeft:
            case Windows.System.VirtualKey.GamepadLeftThumbstickLeft:
                if (this.actualUserRating == Score.NotSet)
                {
                    var a = this.PlaceholderValue;
                    if (a <= 0.5)
                        this.actualUserRating = Score.Score_0_5;
                    else
                        this.actualUserRating = a.ToScore();
                }
                if (this.actualUserRating != Score.Score_0_5)
                {
                    this.actualUserRating--;
                }
                draw();
                break;
            case Windows.System.VirtualKey.Space:
            case Windows.System.VirtualKey.Enter:
            case Windows.System.VirtualKey.GamepadA:
                if (this.actualUserRating != this.UserRatingValue)
                {
                    this.UserRatingValue = this.actualUserRating;
                }
                this.RemoveFocusEngagement();
                break;
            default:
                e.Handled = false;
                break;
            }
        }

        protected override void OnPointerMoved(PointerRoutedEventArgs e)
        {
            base.OnPointerMoved(e);
            if (FocusState == FocusState.Keyboard)
                return;
            e.Handled = true;
            var p = e.GetCurrentPoint(this);
            var pp = p.Position.X / this.ActualWidth;
            if (pp < 0) pp = 0;
            else if (pp > 1) pp = 1;
            this.actualUserRating = (Score)Math.Max((byte)Math.Round(pp * 10), (byte)1);
            draw();
        }

        protected override void OnPointerExited(PointerRoutedEventArgs e)
        {
            base.OnPointerExited(e);
            if (FocusState == FocusState.Keyboard)
                return;
            e.Handled = true;
            this.actualUserRating = this.UserRatingValue;
            draw();
        }

        protected override void OnPointerPressed(PointerRoutedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (FocusState == FocusState.Keyboard)
                return;
            e.Handled = true;
            CapturePointer(e.Pointer);
        }

        protected override void OnPointerReleased(PointerRoutedEventArgs e)
        {
            base.OnPointerReleased(e);
            ReleasePointerCapture(e.Pointer);
            if (FocusState == FocusState.Keyboard)
                return;
            e.Handled = true;
            this.UserRatingValue = this.actualUserRating;
        }

        protected override void OnApplyTemplate()
        {
            this.tbBackground = GetTemplateChild("Background") as TextBlock;
            this.tbPlaceholder = GetTemplateChild("Placeholder") as TextBlock;
            this.tbUserRating = GetTemplateChild("UserRating") as TextBlock;
            this.rgPlaceholderClip = GetTemplateChild("PlaceholderClip") as RectangleGeometry;
            this.rgUserRatingClip = GetTemplateChild("UserRatingClip") as RectangleGeometry;
            base.OnApplyTemplate();
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            draw(finalSize);
            return base.ArrangeOverride(finalSize);
        }

        private void draw() => draw(new Size(ActualWidth, ActualHeight));
        private void draw(Size size)
        {
            var ph = this.PlaceholderValue;
            var ur = this.actualUserRating;
            if (ur > 0)
            {
                if (this.rgPlaceholderClip != null)
                    this.rgPlaceholderClip.Rect = Rect.Empty;
                if (this.rgUserRatingClip != null)
                    this.rgUserRatingClip.Rect = new Rect(0, 0, size.Width / 5 * ur.ToDouble(), size.Height);
            }
            else
            {
                if (this.rgPlaceholderClip != null)
                    this.rgPlaceholderClip.Rect = new Rect(0, 0, size.Width / 5 * ph, size.Height);
                if (this.rgUserRatingClip != null)
                    this.rgUserRatingClip.Rect = Rect.Empty;
            }
        }
    }
}
