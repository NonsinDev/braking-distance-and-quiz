using System.Collections.Concurrent;

namespace QuizHub.Api;

internal sealed class Lobby(string code, string adminConnectionId, int questionDurationSeconds, int maxPlayers)
{
    public string Code { get; } = code;
    public string AdminConnectionId { get; } = adminConnectionId;
    public int QuestionDurationSeconds { get; } = questionDurationSeconds;
    public int MaxPlayers { get; } = maxPlayers;
    public object SyncRoot { get; } = new();
    public ConcurrentDictionary<string, Player> Players { get; } = new();
    public Dictionary<string, Answer> Answers { get; } = [];
    public int CurrentQuestionIndex { get; set; } = -1;
    public int CorrectAnswerIndex { get; set; }
    public DateTimeOffset? QuestionStartedAt { get; set; }
}