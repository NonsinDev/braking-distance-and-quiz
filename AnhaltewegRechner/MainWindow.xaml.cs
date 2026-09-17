using System;
using System.Windows;
using System.Windows.Media.Animation;
using AnhaltewegRechner.ViewModels;
using AnhaltewegRechner.Views;

namespace AnhaltewegRechner;

/// <summary>
/// Übernimmt ausschließlich reine UI-Aufgaben, die sich nicht sauber über
/// Bindings lösen lassen: die pixelgenaue Breite der Vergleichsbalken
/// (abhängig von der tatsächlichen Fensterbreite) sowie das Öffnen der
/// Hilfe-/Formeln-Dialoge. Die eigentliche Berechnung bleibt vollständig
/// im ViewModel gekapselt.
/// </summary>
public partial class MainWindow : Window
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public MainWindow()
    {
        InitializeComponent();

        Loaded += (_, _) => UpdateBars(animate: false);
        SizeChanged += (_, _) => UpdateBars(animate: false);

        ViewModel.PropertyChanged += (_, _) => UpdateBars(animate: false);
        ViewModel.AnimationRequested += (_, _) => UpdateBars(animate: true);
        ViewModel.FormulasRequested += (_, _) => new FormulasWindow { Owner = this }.ShowDialog();
        ViewModel.HelpRequested += (_, _) => new HelpWindow { Owner = this }.ShowDialog();
        ViewModel.ExitRequested += (_, _) => Close();
    }

    private void DryToggle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWetRoad = false;
    }

    private void WetToggle_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.IsWetRoad = true;
    }

    /// <summary>
    /// Berechnet die Zielbreiten der beiden Vergleichsbalken relativ zur
    /// tatsächlich verfügbaren Breite und setzt oder animiert sie.
    /// </summary>
    private void UpdateBars(bool animate)
    {
        if (LimitTrack.ActualWidth <= 0 || OwnTrack.ActualWidth <= 0)
        {
            return;
        }

        double maxDistance = Math.Max(ViewModel.StoppingDistanceMeters, ViewModel.StoppingDistanceLimitMeters);
        if (maxDistance <= 0)
        {
            maxDistance = 1;
        }

        double limitTargetWidth = ViewModel.StoppingDistanceLimitMeters / maxDistance * LimitTrack.ActualWidth;
        double ownTargetWidth = ViewModel.StoppingDistanceMeters / maxDistance * OwnTrack.ActualWidth;

        if (animate)
        {
            AnimateWidth(LimitBarFill, limitTargetWidth);
            AnimateWidth(OwnBarFill, ownTargetWidth);
        }
        else
        {
            LimitBarFill.Width = limitTargetWidth;
            OwnBarFill.Width = ownTargetWidth;
        }
    }

    private static void AnimateWidth(FrameworkElement element, double targetWidth)
    {
        var animation = new DoubleAnimation
        {
            From = 0,
            To = targetWidth,
            Duration = TimeSpan.FromSeconds(1.2),
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        element.BeginAnimation(FrameworkElement.WidthProperty, animation);
    }
}
