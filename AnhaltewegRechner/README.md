# Anhalteweg- und Aufprallgeschwindigkeitsrechner (WPF)

Modernisierte Neuentwicklung der Windows-Anwendung zur Berechnung von
Reaktionsweg, Bremsweg, Anhalteweg und Aufprallgeschwindigkeit. Alle
Funktionen der Altanwendung sind enthalten; die Berechnungslogik wurde
mangels vorhandener Dokumentation durch Testen der Altanwendung
rekonstruiert (siehe `Models/StoppingDistanceCalculator.cs`).

## Voraussetzungen

- Windows 10/11
- Visual Studio 2022 (Workload "Desktopentwicklung mit .NET")
- .NET 8 SDK (wird von Visual Studio in der Regel automatisch mitinstalliert)

## Projekt öffnen und starten

1. `AnhaltewegRechner.sln` in Visual Studio öffnen
2. F5 drücken (oder "Debuggen starten")

Alternativ über die Kommandozeile:

```
dotnet run
```

## Projektstruktur

| Ordner/Datei | Inhalt |
|---|---|
| `Models/StoppingDistanceCalculator.cs` | Reine Berechnungslogik (Formeln), unabhängig von der Oberfläche |
| `ViewModels/MainViewModel.cs` | Zustand, Neuberechnung bei Eingabeänderung, Commands (MVVM) |
| `ViewModels/RelayCommand.cs` | Generische ICommand-Implementierung |
| `MainWindow.xaml(.cs)` | Hauptfenster: Eingaben, Ergebniskarten, Balkenvergleich, Animation |
| `Views/HelpWindow.xaml(.cs)` | Dialog "Hilfe" |
| `Views/FormulasWindow.xaml(.cs)` | Dialog "Formeln" |
| `Themes/Colors.xaml`, `Themes/Styles.xaml` | Zentrale Farbpalette und Steuerelement-Styles für das moderne Design |
| `Converters/InverseBooleanConverter.cs` | Hilfskonverter für den Trocken/Nass-Umschalter |

## Funktionsumfang (unverändert gegenüber der Altanwendung)

- Einstellung von zulässiger Höchstgeschwindigkeit, eigener Geschwindigkeit,
  Reaktionszeit und Fahrbahnzustand (trocken/nass)
- Anzeige von Reaktionsweg, Bremsweg, Anhalteweg und Aufprallgeschwindigkeit
- Erklärender Hinweistext bei Geschwindigkeitsüberschreitung
- Grafischer Vergleich der Anhaltewege beider Geschwindigkeiten inkl. Animation
- Menüpunkte "Formeln", "Hilfe" und "Beenden"

## Hinweis zur Bremsverzögerung

Für die Bremswegberechnung werden 7,5 m/s² (trocken) bzw. 5,0 m/s² (nass)
angesetzt. Das sind typische, in der Verkehrspädagogik gebräuchliche
Richtwerte für eine Vollbremsung – siehe Dialog "Formeln" in der Anwendung.
