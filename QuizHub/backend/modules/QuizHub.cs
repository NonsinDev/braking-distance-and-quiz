using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace QuizHub.Api;

public sealed class QuizHub(IQuizRepository quizRepository) : Hub
{
    private static readonly ConcurrentDictionary<string, Lobby> Lobbies = new();
    private static readonly ConcurrentDictionary<string, string> ConnectionLobbies = new();
    private static readonly Random Random = new();
    private static readonly object RandomLock = new();

    public Task<IReadOnlyList<QuizSummaryDto>> GetAvailableQuizzes()
    {
        return Task.FromResult(quizRepository.GetAvailableQuizzes());
    }

    public async Task<LobbyCreated> CreateLobby(string? quizId, int questionDurationSeconds, int maxPlayers)
    {
        if (questionDurationSeconds is < 10 or > 600)
            throw new HubException("Die Fragezeit muss zwischen 10 und 600 Sekunden liegen.");
        if (maxPlayers is < 1 or > 100)
            throw new HubException("Es sind maximal 100 Spieler möglich.");

        var quiz = quizRepository.GetQuiz(quizId)
            ?? throw new HubException("Kein passendes Quiz gefunden.");

        string code;
        do
        {
            lock (RandomLock)
            {
                code = Random.Next(1000, 10000).ToString();
            }
        } while (!Lobbies.TryAdd(code, new Lobby(code, Context.ConnectionId, quiz, questionDurationSeconds, maxPlayers)));

        ConnectionLobbies[Context.ConnectionId] = code;
        await Groups.AddToGroupAsync(Context.ConnectionId, code);
        return new LobbyCreated(code, Context.ConnectionId, quiz.Id, quiz.Name, quiz.Questions.Count);
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
        await Clients.Caller.SendAsync("LobbyJoined", new LobbyDto(code, lobby.Quiz.Name, lobby.Players.Values.Select(ToDto).ToArray()));
    }

    public async Task<QuestionStartedDto> StartNextQuestion(string code, int questionIndex)
    {
        var lobby = GetLobbyForAdmin(code);
        QuizQuestion question;
        int timeLimit;
        lock (lobby.SyncRoot)
        {
            if (questionIndex < 0 || questionIndex >= lobby.Quiz.Questions.Count)
                throw new HubException("Ungültiger Frage-Index.");

            question = lobby.Quiz.Questions[questionIndex];
            lobby.CurrentQuestionIndex = questionIndex;
            lobby.CorrectAnswerIndices = question.CorrectIndices;
            timeLimit = lobby.QuestionDurationSeconds;
            lobby.ActiveTimeLimitSeconds = timeLimit;
            lobby.QuestionStartedAt = DateTimeOffset.UtcNow;
            lobby.Answers.Clear();
        }

        var questionDto = new QuestionStartedDto(
            questionIndex,
            lobby.Quiz.Questions.Count,
            question.Question,
            question.Options.Select(o => o.Text).ToArray(),
            timeLimit,
            question.HasMultipleCorrectAnswers,
            question.CorrectIndices.Length,
            question.MediaUrl,
            lobby.QuestionStartedAt.Value
        );

        await Clients.Group(lobby.Code).SendAsync("QuestionStarted", questionDto);
        await Clients.Client(lobby.AdminConnectionId).SendAsync("AnswersUpdated", new AnswersProgress(0, lobby.Players.Count));
        return questionDto;
    }

    public async Task<QuestionEndedDto> RevealQuestion(string code)
    {
        var lobby = GetLobbyForAdmin(code);
        int questionIndex;
        int[] correctIndices;
        lock (lobby.SyncRoot)
        {
            questionIndex = lobby.CurrentQuestionIndex;
            correctIndices = lobby.CorrectAnswerIndices;
        }

        var dto = new QuestionEndedDto(questionIndex, correctIndices);
        await Clients.Group(lobby.Code).SendAsync("QuestionEnded", dto);
        return dto;
    }

    public async Task EndQuiz(string code)
    {
        var lobby = GetLobbyForAdmin(code);
        var scores = lobby.Players.Values.Select(ToDto).OrderByDescending(player => player.Score).ToArray();
        await Clients.Group(lobby.Code).SendAsync("QuizEnded", scores);
    }

    public Task SubmitSingleAnswer(string code, int selectedOption)
    {
        return SubmitAnswer(code, [selectedOption]);
    }

    public async Task SubmitAnswer(string code, int[] selectedOptions)
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
            var isWithinTimeLimit = elapsedSeconds <= (lobby.ActiveTimeLimitSeconds + 1.5);

            var correctSet = lobby.CorrectAnswerIndices.ToHashSet();
            var selectedSet = (selectedOptions ?? []).Distinct().ToHashSet();

            bool isFullyCorrect = isWithinTimeLimit && selectedSet.SetEquals(correctSet);
            bool hasWrongSelection = selectedSet.Any(s => !correctSet.Contains(s));
            int correctChosen = selectedSet.Count(s => correctSet.Contains(s));
            bool isPartial = isWithinTimeLimit && !isFullyCorrect && !hasWrongSelection && correctChosen > 0;

            int duration = Math.Max(1, lobby.ActiveTimeLimitSeconds);
            int basePoints = isWithinTimeLimit ? Math.Max(100, (int)Math.Round(1000 - (elapsedSeconds / duration) * 500)) : 0;
            int points = 0;

            if (isFullyCorrect)
            {
                points = basePoints;
            }
            else if (isPartial && correctSet.Count > 0)
            {
                points = (int)Math.Round(basePoints * ((double)correctChosen / correctSet.Count));
            }

            player.Score += points;
            lobby.Answers[Context.ConnectionId] = new Answer(selectedOptions ?? [], isFullyCorrect, points, isPartial);
            result = new AnswerResult(isFullyCorrect, points, isPartial);
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
