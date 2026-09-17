using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using AnhaltewegRechner.Models;

namespace AnhaltewegRechner.ViewModels;

/// <summary>
/// Zentrales ViewModel der Anwendung. Hält alle Eingabewerte, berechnet
/// bei jeder Änderung sofort alle abgeleiteten Ergebnisse neu und stellt
/// die Commands für die Kopfzeile (Hilfe, Formeln, Beenden) sowie die
/// Animation bereit. Enthält bewusst keinerlei Verweise auf WPF-Fenster,
/// um Oberfläche und Logik sauber zu trennen (MVVM).
/// </summary>
public class MainViewModel : INotifyPropertyChanged
{
    private double _speedLimitKmh = 30;
    private double _ownSpeedKmh = 50;
    private double _reactionTimeSeconds = 1.0;
    private bool _isWetRoad;

    public MainViewModel()
    {
        PlayAnimationCommand = new RelayCommand(() => AnimationRequested?.Invoke(this, EventArgs.Empty));
        ShowFormulasCommand = new RelayCommand(() => FormulasRequested?.Invoke(this, EventArgs.Empty));
        ShowHelpCommand = new RelayCommand(() => HelpRequested?.Invoke(this, EventArgs.Empty));
        ExitCommand = new RelayCommand(() => ExitRequested?.Invoke(this, EventArgs.Empty));

        Recalculate();
    }

    // ---------- Eingaben ----------

    public double SpeedLimitKmh
    {
        get => _speedLimitKmh;
        set
        {
            if (SetField(ref _speedLimitKmh, Math.Round(value))) Recalculate();
        }
    }

    public double OwnSpeedKmh
    {
        get => _ownSpeedKmh;
        set
        {
            if (SetField(ref _ownSpeedKmh, Math.Round(value))) Recalculate();
        }
    }

    public double ReactionTimeSeconds
    {
        get => _reactionTimeSeconds;
        set
        {
            if (SetField(ref _reactionTimeSeconds, Math.Round(value, 1))) Recalculate();
        }
    }

    public bool IsWetRoad
    {
        get => _isWetRoad;
        set
        {
            if (SetField(ref _isWetRoad, value))
            {
                OnPropertyChanged(nameof(RoadConditionLabel));
                OnPropertyChanged(nameof(RoadConditionIcon));
                Recalculate();
            }
        }
    }

    public string RoadConditionLabel => IsWetRoad ? "Nass" : "Trocken";
    public string RoadConditionIcon => IsWetRoad ? "🌧" : "☀";

    // ---------- Ergebnisse: eigenes Fahrzeug ----------

    public double ReactionDistanceMeters { get; private set; }
    public double BrakingDistanceMeters { get; private set; }
    public double StoppingDistanceMeters { get; private set; }
    public double ImpactSpeedKmh { get; private set; }

    // ---------- Ergebnisse: Vergleichsfahrzeug (zulässige Höchstgeschwindigkeit) ----------

    public double ReactionDistanceLimitMeters { get; private set; }
    public double BrakingDistanceLimitMeters { get; private set; }
    public double StoppingDistanceLimitMeters { get; private set; }

    // ---------- Abgeleitete Anzeige-Eigenschaften ----------

    public bool IsSpeeding => OwnSpeedKmh > SpeedLimitKmh;

    public double SpeedDifferenceKmh => Math.Max(0, OwnSpeedKmh - SpeedLimitKmh);

    public string SpeedWarningText => IsSpeeding
        ? $"Sie fahren {SpeedDifferenceKmh:0} km/h zu schnell!"
        : "Sie halten die zulässige Höchstgeschwindigkeit ein.";

    public string ExplanationText => IsSpeeding
        ? $"Sie fahren mit {OwnSpeedKmh:0} km/h. Erlaubt sind {SpeedLimitKmh:0} km/h. Das bedeutet: Sie prallen mit " +
          $"{ImpactSpeedKmh:0.0} km/h auf ein Hindernis, an dem ein Fahrzeug mit zulässiger Höchstgeschwindigkeit " +
          "bereits zum Stillstand gekommen wäre."
        : $"Sie fahren mit {OwnSpeedKmh:0} km/h und halten die zulässige Höchstgeschwindigkeit von " +
          $"{SpeedLimitKmh:0} km/h ein.";

    // ---------- Commands ----------

    public ICommand PlayAnimationCommand { get; }
    public ICommand ShowFormulasCommand { get; }
    public ICommand ShowHelpCommand { get; }
    public ICommand ExitCommand { get; }

    // ---------- Events (von MainWindow abonniert, um UI-Effekte auszulösen) ----------

    public event EventHandler? AnimationRequested;
    public event EventHandler? FormulasRequested;
    public event EventHandler? HelpRequested;
    public event EventHandler? ExitRequested;

    // ---------- Berechnung ----------

    private void Recalculate()
    {
        RoadCondition condition = IsWetRoad ? RoadCondition.Wet : RoadCondition.Dry;

        ReactionDistanceMeters = StoppingDistanceCalculator.ReactionDistanceMeters(OwnSpeedKmh, ReactionTimeSeconds);
        BrakingDistanceMeters = StoppingDistanceCalculator.BrakingDistanceMeters(OwnSpeedKmh, condition);
        StoppingDistanceMeters = ReactionDistanceMeters + BrakingDistanceMeters;
        ImpactSpeedKmh = StoppingDistanceCalculator.ImpactSpeedKmh(OwnSpeedKmh, SpeedLimitKmh, ReactionTimeSeconds, condition);

        ReactionDistanceLimitMeters = StoppingDistanceCalculator.ReactionDistanceMeters(SpeedLimitKmh, ReactionTimeSeconds);
        BrakingDistanceLimitMeters = StoppingDistanceCalculator.BrakingDistanceMeters(SpeedLimitKmh, condition);
        StoppingDistanceLimitMeters = ReactionDistanceLimitMeters + BrakingDistanceLimitMeters;

        // Aktualisiert alle nicht einzeln gemeldeten, berechneten Eigenschaften auf einen Schlag.
        OnPropertyChanged(string.Empty);
    }

    // ---------- INotifyPropertyChanged-Infrastruktur ----------

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
