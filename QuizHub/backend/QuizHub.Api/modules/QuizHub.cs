using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace QuizHub.Api;

public sealed class QuizHub : Hub
{
    private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionLobbies = new();
    private static readonly Random Random = new();
    private static readonly object RandomLock = new();

    public Task<LobbyCreated> CreateLobby(int questionDurationSeconds, int maxPlayers)
    {
        if (questionDurationSeconds is < 10 or > 600)
            throw new HubException("Die Fragezeit muss zwischen 10 und 600 Sekunden liegen.");
        if (maxPlayers is < 1 or > 100)
            throw new HubException("Es sind maximal 100 Spieler möglich.");

        string code;
        do
        {
            lock (RandomLock)
            {
                code = Random.Next(1000, 10000).ToString();
            }
        } while (!Lobbies.TryAdd(code, new Lobby(code, Context.ConnectionId, questionDurationSeconds, maxPlayers)));

        ConnectionLobbies[Context.ConnectionId] = code;
        return Task.FromResult(new LobbyCreated(code, Context.ConnectionId));
    }

    public async Task JoinLobby(string code, string playerName)
    {
        code = NormalizeCode(code);
        playerName = playerName.Trim();
        if (playerName.Length is < 1 or > 40)
            throw new HubException("Der Name muss zwischen 1 und 40 Zeichen lang sein.");
        if (!Lobbies.TryGetValue(code, out var lobby))
            throw new HubException("Dieser Raum wurde nicht gefunden.");

        var player = new Player(Context.ConnectionId, playerName);
        lock (lobby.SyncRoot)
        {
            if (lobby.Players.Count >= lobby.MaxPlayers)
                throw new HubException("Dieser Quizraum ist bereits voll.");
            if (lobby.Players.Values.Any(item => item.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase)))
                throw new HubException("Dieser Name ist bereits vergeben.");
            lobby.Players[Context.ConnectionId] = player;
        }

        ConnectionLobbies[Context.ConnectionId] = code;
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        await Clients.Client(lobby.AdminConnectionId).SendAsync("PlayerJoined", new PlayerDto(player.Id, player.Name));
        await Clients.Caller.SendAsync("LobbyJoined", new LobbyDto(code, lobby.Players.Values.Select(ToDto).ToArray()));
    }

    public async Task StartNextQuestion(string code, int questionIndex, int correctAnswerIndex)
    {
        var lobby = GetLobbyForAdmin(code);
        lock (lobby.SyncRoot)
        {
            lobby.CurrentQuestionIndex = questionIndex;
            lobby.CorrectAnswerIndex = correctAnswerIndex;
            lobby.QuestionStartedAt = DateTimeOffset.UtcNow;
            lobby.Answers.Clear();
        }

        await Clients.Group(lobby.Code).SendAsync("QuestionStarted", new
        {
            questionIndex,
            startedAt = lobby.QuestionStartedAt,
        });
        await Clients.Client(lobby.AdminConnectionId).SendAsync("AnswersUpdated", new AnswersProgress(0, lobby.Players.Count));
    }

    public async Task EndQuiz(string code)
    {
        var lobby = GetLobbyForAdmin(code);
        var scores = lobby.Players.Values.Select(ToDto).OrderByDescending(player => player.Score).ToArray();
        await Clients.Group(lobby.Code).SendAsync("QuizEnded", scores);
    }

    public async Task SubmitAnswer(string code, int selectedOption)
    {
        if (!Lobbies.TryGetValue(NormalizeCode(code), out var lobby))
            throw new HubException("Dieser Raum wurde nicht gefunden.");
        if (!lobby.Players.TryGetValue(Context.ConnectionId, out var player))
            throw new HubException("Du bist nicht als Spieler registriert.");

        AnswerResult result;
        AnswersProgress progress;
        lock (lobby.SyncRoot)
        {
            if (lobby.QuestionStartedAt is null || lobby.Answers.ContainsKey(Context.ConnectionId))
                return;

            var elapsedSeconds = (DateTimeOffset.UtcNow - lobby.QuestionStartedAt.Value).TotalSeconds;
            var isWithinTimeLimit = elapsedSeconds < lobby.QuestionDurationSeconds;
            var isCorrect = isWithinTimeLimit && selectedOption == lobby.CorrectAnswerIndex;
            var points = isCorrect ? Math.Max(0, (int)Math.Round(1000 - elapsedSeconds * 50)) : 0;
            player.Score += points;
            lobby.Answers[Context.ConnectionId] = new Answer(selectedOption, isCorrect, points);
            result = new AnswerResult(isCorrect, points);
            progress = new AnswersProgress(lobby.Answers.Count, lobby.Players.Count);
        }

        await Clients.Caller.SendAsync("AnswerAccepted", result);
        await Clients.Client(lobby.AdminConnectionId).SendAsync("AnswersUpdated", progress);
        await Clients.Client(lobby.AdminConnectionId).SendAsync("ScoresUpdated", lobby.Players.Values.Select(ToDto).ToArray());
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (ConnectionLobbies.TryRemove(Context.ConnectionId, out var code) && Lobbies.TryGetValue(code, out var lobby))
        {
            var wasAdmin = lobby.AdminConnectionId == Context.ConnectionId;
            lobby.Players.TryRemove(Context.ConnectionId, out _);
            if (!wasAdmin)
                await Clients.Client(lobby.AdminConnectionId).SendAsync("PlayerLeft", Context.ConnectionId);
            else
                Lobbies.TryRemove(code, out _);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private Lobby GetLobbyForAdmin(string code)
    {
        if (!Lobbies.TryGetValue(NormalizeCode(code), out var lobby) || lobby.AdminConnectionId != Context.ConnectionId)
            throw new HubException("Nur die Quizleitung darf diese Aktion ausführen.");
        return lobby;
    }

    private static string NormalizeCode(string code) => code.Trim();
    private static PlayerDto ToDto(Player player) => new(player.Id, player.Name, player.Score);
}