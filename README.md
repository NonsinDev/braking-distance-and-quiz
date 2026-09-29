# Braking Distance & QuizHub

Dieses Repository vereint zwei eigenständige, moderne Softwareprojekte im Bereich Verkehrssicherheit, Physik und interaktives Lernen:

1. **AnhaltewegRechner**: Eine moderne Windows-Desktopanwendung (WPF / .NET 8) zur präzisen Berechnung und visuellen Darstellung von Reaktionsweg, Bremsweg, Anhalteweg und Aufprallgeschwindigkeit bei Geschwindigkeitsüberschreitungen.
2. **QuizHub**: Eine interaktive Live-Multiplayer-Quiz-Plattform im Kahoot-Stil (ASP.NET Core SignalR + Vue 3) mit modularen, themenspezifischen Fragenkatalogen (unter anderem zu Fahrsicherheit und Allgemeinwissen).

---

## Inhaltsverzeichnis

- [Übersicht der Teilprojekte](#übersicht-der-teilprojekte)
- [Projektstruktur](#projektstruktur)
- [Systemvoraussetzungen](#systemvoraussetzungen)
- [Schnellstart](#schnellstart)
  - [1. AnhaltewegRechner starten](#1-anhaltewegrechner-starten)
  - [2. QuizHub starten](#2-quizhub-starten)
- [Projekt-Artefakte & Dokumentation](#projekt-artefakte--dokumentation)

---

## Übersicht der Teilprojekte

### 1. AnhaltewegRechner (WPF)

Der AnhaltewegRechner ist eine moderne Neuentwicklung einer Verkehrssicherheits-Anwendung auf Basis von .NET 8 und WPF (Windows Presentation Foundation) nach dem MVVM-Muster.

- **Physikalische Berechnungen**:
  - **Reaktionsweg**: $s_{\text{Reaktion}} = v \cdot t_{\text{Reaktion}}$
  - **Bremsweg**: $s_{\text{Brems}} = \frac{v^2}{2 \cdot a}$ (mit $a = 7{,}5\,\text{m/s}^2$ bei trockener bzw. $5{,}0\,\text{m/s}^2$ bei nasser Fahrbahn)
  - **Anhalteweg**: $s_{\text{Anhalt}} = s_{\text{Reaktion}} + s_{\text{Brems}}$
  - **Aufprallgeschwindigkeit**: Physikalisch exakte Ermittlung der verbleibenden Restgeschwindigkeit am Gefahrenpunkt bei Geschwindigkeitsüberschreitung.
- **Visualisierung & Interaktion**:
  - Dynamischer Balkenvergleich zwischen zulässiger und tatsächlich gefahrener Geschwindigkeit.
  - Anschauliche Bremsanimation zur Verdeutlichung des Unterschieds.
  - Detaillierte Hinweiskarten und Formelübersichten.
- Ausführliche Dokumentation: [AnhaltewegRechner/README.md](AnhaltewegRechner/README.md)

### 2. QuizHub (Live-Multiplayer)

QuizHub ist eine vollständige Echtzeit-Webanwendung, mit der Schulungsleiter oder Moderatoren ein interaktives Quiz für Gruppen veranstalten können.

- **Echtzeit-Synchronisation**: SignalR-Hub für blitzschnelle Kommunikation zwischen Spielleitung und Teilnehmenden ohne Seiten-Reloads.
- **Teilnahme ohne Installation**: Spieler treten über Browser auf Smartphone, Tablet oder Desktop per Raumcode oder QR-Code bei.
- **Modulare Quizzes**: Fragenkataloge werden als eigenständige JSON-Dateien verwaltet und zur Laufzeit dynamisch geladen (z. B. zu Fahrsicherheit, Allgemeinwissen, Gaming & Popkultur sowie Wissenschaft & Technik).
- **Features**: Single- und Multiple-Choice-Fragen, Countdown-Timer, adaptive Punktwertung nach Reaktionszeit, Zwischenstände und animiertes Siegerpodest.
- Ausführliche Dokumentation: [QuizHub/README.md](QuizHub/README.md)

---

## Projektstruktur

```text
braking-distance-and-quiz/
├── AnhaltewegRechner/            # WPF-Desktopanwendung (.NET 8)
│   ├── Models/                  # Berechnungslogik (StoppingDistanceCalculator)
│   ├── ViewModels/              # MVVM-Zustands- & Command-Logik
│   ├── Views/                   # Zusätzliche Fenster (Formeln, Hilfe)
│   ├── Themes/                  # XAML-Farben und Styles
│   ├── MainWindow.xaml          # Hauptbenutzeroberfläche
│   ├── AnhaltewegRechner.csproj # Projektdatei
│   ├── AnhaltewegRechner.sln    # Visual Studio Projektmappe
│   └── README.md                # Dokumentation AnhaltewegRechner
│
├── QuizHub/                     # Live-Quiz-Plattform (Fullstack)
│   ├── backend/                 # ASP.NET Core 8 Web API & SignalR-Hub
│   │   ├── modules/             # Lobby-, Player- und Quiz-Modelle
│   │   ├── Program.cs           # API- & Hub-Konfiguration
│   │   └── QuizHub.Api.csproj   # Backend-Projektdatei
│   ├── frontend/                # Vue 3 + Vite Frontend (SPA)
│   │   ├── src/                 # Views (AdminView, PlayerView), SignalR-Composable
│   │   └── package.json         # Frontend-Abhängigkeiten & Skripte
│   ├── quizzes/                 # JSON-basierte Quiz-Kataloge
│   └── README.md                # Dokumentation QuizHub
│
├── WireframesBracingDistance.odp # UI/UX-Wireframes und Entwurfskonzepte
├── Zeitplanung.pdf              # Zeit- und Phasenplanung des Projekts
└── README.md                    # Diese Repository-Übersicht
```

---

## Systemvoraussetzungen

- **Betriebssystem**:
  - AnhaltewegRechner: Windows 10 / 11
  - QuizHub: Plattformunabhängig (Windows, macOS, Linux)
- **Entwicklungsumgebung & Runtimes**:
  - [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
  - [Node.js](https://nodejs.org/) (Version 20 oder neuer) & npm (für das QuizHub-Frontend)
  - Optional: Visual Studio 2022 (mit Workload *Desktopentwicklung mit .NET*) oder VS Code

---

## Schnellstart

### 1. AnhaltewegRechner starten

Das Projekt kann entweder direkt in Visual Studio über [AnhaltewegRechner/AnhaltewegRechner.sln](AnhaltewegRechner/AnhaltewegRechner.sln) geöffnet oder per Terminal gestartet werden:

```powershell
dotnet run --project AnhaltewegRechner/AnhaltewegRechner.csproj
```

### 2. QuizHub starten

Für QuizHub werden sowohl das Backend als auch das Frontend gestartet:

**Schritt A – Backend ausführen:**

```powershell
dotnet run --project QuizHub/backend/QuizHub.Api.csproj
```
Das Backend startet standardmäßig unter `http://localhost:5000` (SignalR-Hub unter `/quizHub`, REST-API unter `/api/quizzes`).

**Schritt B – Frontend ausführen:**

In einem separaten Terminal:

```powershell
cd QuizHub/frontend
npm install
npm run dev
```
Das Web-Frontend steht anschließend unter `http://localhost:5173` zur Verfügung.

---

## Projekt-Artefakte & Dokumentation

- [AnhaltewegRechner/README.md](AnhaltewegRechner/README.md): Detaillierte Dokumentation zur Berechnungslogik, Formeln, Bremsverzögerungswerten und Projektstruktur der Desktopanwendung.
- [QuizHub/README.md](QuizHub/README.md): Detaillierte Dokumentation zu SignalR-Ereignissen, Quiz-JSON-Format, Multiple-Choice-Erkennung und Produktiv-Build.
- [WireframesBracingDistance.odp](WireframesBracingDistance.odp): Präsentationsdatei mit den ursprünglichen Wireframes und Oberflächenentwürfen.
- [Zeitplanung.pdf](Zeitplanung.pdf): Dokumentation des Projektzeitplans und der Meilensteine.
 
