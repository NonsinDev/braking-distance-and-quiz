# QuizHub

QuizHub ist ein Echtzeit-Quiz fuer Fahrsicherheitstrainings. Eine Moderation erstellt einen Quizraum, waehlt die Spielparameter und steuert den Ablauf. Teilnehmende treten mit einem Raumcode oder QR-Code bei und beantworten die Fragen auf ihrem eigenen Geraet.

Die Anwendung besteht aus einem ASP.NET-Core-Backend mit SignalR und einem Vue-3-Frontend.

## Funktionen

- Quizraeume mit automatisch generiertem Raumcode
- Beitritt fuer Teilnehmende per Raumcode oder QR-Code
- Echtzeit-Kommunikation zwischen Moderation und Teilnehmenden mit SignalR
- Konfigurierbare Zeit pro Frage und maximale Spielerzahl
- Fortschrittsanzeige fuer eingegangene Antworten
- Punkteberechnung und Siegerpodest am Quizende
- Optionale Medien pro Frage
- Health-Endpoint fuer die Backend-Pruefung

## Voraussetzungen

- .NET 8 SDK
- Node.js 20 oder neuer
- npm

## Projekt lokal starten

### 1. Backend starten

Im Projektverzeichnis:

```powershell
dotnet run --project backend/QuizHub.Api/QuizHub.Api.csproj
```

Das Backend stellt den SignalR-Hub unter `http://localhost:5000/quizHub` bereit. Der Health-Endpoint ist unter `http://localhost:5000/health` erreichbar.

### 2. Frontend starten

In einem zweiten Terminal:

```powershell
cd frontend
npm install
npm run dev
```

Danach ist das Frontend normalerweise unter `http://localhost:5173` erreichbar.

## Verwendung

### Moderation

1. `http://localhost:5173/?mode=admin` oeffnen.
2. Zeit pro Frage und maximale Spielerzahl festlegen.
3. Einen Quizraum erstellen.
4. Den angezeigten Raumcode oder QR-Code an die Teilnehmenden weitergeben.
5. Das Quiz starten und die Fragen steuern.

### Teilnehmende

1. `http://localhost:5173/` oeffnen oder den QR-Code scannen.
2. Raumcode und Namen eingeben.
3. Dem Quiz beitreten.
4. Antworten direkt auf dem eigenen Geraet abgeben.

## Konfiguration

### SignalR-URL

Standardmaessig verwendet das Frontend den relativen Pfad `/quizHub`. Wenn das Backend unter einer anderen Adresse laeuft, kann die URL ueber `VITE_HUB_URL` gesetzt werden:

```powershell
$env:VITE_HUB_URL = "https://quiz-api.example.com/quizHub"
npm run dev
```

### CORS

Erlaubte Frontend-Urspruenge werden in `backend/QuizHub.Api/appsettings.json` unter `AllowedOrigins` gepflegt. Fuer eine Produktion muss dort die tatsaechliche Frontend-Domain eingetragen werden.

Beispiel:

```json
{
	"AllowedOrigins": [
		"https://quiz.example.com"
	]
}
```

## Fragen bearbeiten

Die Fragen werden aus `frontend/src/questions.json` geladen. Jede Frage kann folgende Eigenschaften enthalten:

```json
{
	"id": 1,
	"question": "Fragetext",
	"mediaUrl": null,
	"options": ["Antwort A", "Antwort B", "Antwort C", "Antwort D"],
	"correctIndex": 1,
	"timeLimitSeconds": 20
}
```

`correctIndex` ist der nullbasierte Index der richtigen Antwort. Die aktuell eingestellte Zeit pro Frage wird von der Moderation festgelegt.

## Projektstruktur

```text
backend/QuizHub.Api/   ASP.NET-Core-API und SignalR-Hub
frontend/src/          Vue-Anwendung
frontend/src/views/    Ansichten fuer Moderation und Teilnehmende
frontend/src/questions.json
											 Fragenkatalog
```

## Build

Frontend fuer die Auslieferung bauen:

```powershell
cd frontend
npm run build
```

Backend bauen:

```powershell
dotnet build backend/QuizHub.Api/QuizHub.Api.csproj
```

Bei einer Produktion sollten Frontend und Backend ueber HTTPS ausgeliefert werden. Die CORS-Konfiguration und `VITE_HUB_URL` muessen dabei auf die tatsaechlichen Domains und Pfade zeigen.
