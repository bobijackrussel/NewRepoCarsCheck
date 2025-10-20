using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace CarRentalManagment.Views
{
    public partial class VehicleListView : UserControl
    {
        public VehicleListView()
        {
            InitializeComponent();
        }

        private void FiltersButton_Click(object sender, RoutedEventArgs e)
        {
            // Toggle filter sidebar visibility
            if (FilterSidebar.Visibility == Visibility.Collapsed)
            {
                FilterSidebar.Visibility = Visibility.Visible;

                // Animate slide in
                var slideIn = new DoubleAnimation
                {
                    From = 340,
                    To = 0,
                    Duration = TimeSpan.FromMilliseconds(250),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };

                var translateTransform = new TranslateTransform();
                FilterSidebar.RenderTransform = translateTransform;
                translateTransform.BeginAnimation(TranslateTransform.XProperty, slideIn);
            }
        }

        private void CloseFilters_Click(object sender, RoutedEventArgs e)
        {
            // Animate slide out
            var slideOut = new DoubleAnimation
            {
                From = 0,
                To = 340,
                Duration = TimeSpan.FromMilliseconds(250),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            slideOut.Completed += (s, args) =>
            {
                FilterSidebar.Visibility = Visibility.Collapsed;
            };

            var translateTransform = FilterSidebar.RenderTransform as TranslateTransform;
            if (translateTransform == null)
            {
                translateTransform = new TranslateTransform();
                FilterSidebar.RenderTransform = translateTransform;
            }

            translateTransform.BeginAnimation(TranslateTransform.XProperty, slideOut);
        }

        private void Card_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                // Subtle hover effect
                var scaleTransform = new ScaleTransform(1.0, 1.0);
                border.RenderTransform = scaleTransform;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                var scaleUpX = new DoubleAnimation(1.0, 1.02, TimeSpan.FromMilliseconds(200));
                var scaleUpY = new DoubleAnimation(1.0, 1.02, TimeSpan.FromMilliseconds(200));

                scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleUpX);
                scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleUpY);
            }
        }

        private void Card_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                var scaleTransform = border.RenderTransform as ScaleTransform;
                if (scaleTransform != null)
                {
                    var scaleDown = new DoubleAnimation(1.02, 1.0, TimeSpan.FromMilliseconds(200));
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleDown);
                    scaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleDown);
                }
            }
        }

    }
}
