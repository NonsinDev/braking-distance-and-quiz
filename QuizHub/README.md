# QuizHub — Live Multiplayer Quiz

QuizHub ist eine vielseitige, interaktive Echtzeit-Quiz-Plattform im Stil von Kahoot. Eine Spielleitung erstellt einen Raum, wählt eines der modularen Quizzes aus und steuert den Ablauf. Teilnehmende treten einfach per Raumcode oder QR-Code mit Smartphone, Tablet oder Laptop bei und stimmen live ab.

Die Anwendung besteht aus einem ASP.NET-Core-Backend mit SignalR und einem modernen Vue-3-Frontend.

## Funktionen

- **Modulare Quiz-Verwaltung:** Quizzes werden als separate JSON-Dateien im Ordner `quizzes/` abgelegt und automatisch geladen.
- **Mehrfachauswahl (Multiple Correct Answers):** Fragen können eine oder mehrere richtige Antworten haben. Wenn mehrere Antworten richtig sind, wird dies sowohl Moderation als auch Teilnehmenden prominent angezeigt, inklusive Mehrfachauswahl-Buttons.
- **Kahoot-Feeling:** Schnelle Punktevergabe basierend auf Antwortgeschwindigkeit, Live-Countdown, Answer-Reveal und ein dynamisches Siegerpodest.
- **Echtzeit-SignalR:** Automatische Synchronisation von Raumcodes, Fragen, Antworten und Punkteständen ohne Neuladen.
- **Flexibles Hosting:** Einstellbare Zeitlimits pro Frage, Maximalspieler und automatische Erkennung neuer Quizzes.

## Voraussetzungen

- .NET 8 SDK
- Node.js 20 oder neuer
- npm

## Projekt lokal starten

### 1. Backend starten

Im Verzeichnis `QuizHub`:

```powershell
dotnet run --project backend/QuizHub.Api/QuizHub.Api.csproj
```

Das Backend scannt automatisch den Ordner `quizzes/`, stellt die API unter `http://localhost:5000/api/quizzes` und den SignalR-Hub unter `http://localhost:5000/quizHub` bereit.

### 2. Frontend starten

In einem zweiten Terminal im Verzeichnis `QuizHub/frontend`:

```powershell
npm install
npm run dev
```

Danach ist das Frontend unter `http://localhost:5173` erreichbar.

## Quizzes erstellen & anpassen

Alle Quizzes liegen als `.json`-Dateien im Ordner `QuizHub/quizzes/`. Du kannst beliebig viele eigene Quizzes hinzufügen!

### Quiz-Aufbau (JSON)

```json
{
  "name": "Dein Quizname",
  "description": "Optionale Beschreibung des Quizthemas",
  "questions": [
    {
      "question": "Welche Antwort ist hier richtig?",
      "options": [
        { "text": "Antwort 1", "isCorrect": true },
        { "text": "Antwort 2", "isCorrect": false },
        { "text": "Antwort 3", "isCorrect": false },
        { "text": "Antwort 4", "isCorrect": false }
      ],
      "timeLimitSeconds": 20
    },
    {
      "question": "Welche dieser Aussagen treffen zu? (Mehrere richtig)",
      "options": [
        { "text": "Antwort A", "isCorrect": false },
        { "text": "Antwort B", "isCorrect": true },
        { "text": "Antwort C", "isCorrect": true },
        { "text": "Antwort D", "isCorrect": false }
      ],
      "timeLimitSeconds": 25
    }
  ]
}
```

- **Mehrere richtige Antworten:** Wenn 2 oder mehr Optionen `"isCorrect": true` haben, erkennt das System die Frage automatisch als Mehrfachauswahl und signalisiert dies allen Spielern.
- **Zeitlimit:** Über `timeLimitSeconds` kann jede Frage ihr eigenes Zeitlimit erhalten.

## Projektstruktur

```text
quizzes/               Modulare Quiz-Dateien (z. B. allgemeinwissen.json, gaming-und-popkultur.json)
backend/QuizHub.Api/   ASP.NET-Core-API, SignalR-Hub & Quiz-Repository
frontend/src/          Vue-Anwendung
frontend/src/views/    Ansichten fuer Moderation (AdminView) und Teilnehmende (PlayerView)
```

## Build

Frontend für die Produktion bauen:

```powershell
cd frontend
npm run build
```

Backend bauen:

```powershell
dotnet build backend/QuizHub.Api/QuizHub.Api.csproj
```

Bei einer Produktion sollten Frontend und Backend ueber HTTPS ausgeliefert werden. Die CORS-Konfiguration und `VITE_HUB_URL` muessen dabei auf die tatsaechlichen Domains und Pfade zeigen.
