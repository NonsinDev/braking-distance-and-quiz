namespace QuizHub.Api;

public sealed record LobbyCreated(string Code, string AdminConnectionId, string QuizId, string QuizTitle, int TotalQuestions = 0);
public sealed record PlayerDto(string Id, string Name, int Score = 0);
public sealed record LobbyDto(string Code, string QuizTitle, PlayerDto[] Players);
public sealed record AnswersProgress(int Answered, int Total);
public sealed record AnswerResult(bool IsCorrect, int Points, bool IsPartial = false);
public sealed record QuizSummaryDto(string Id, string Name, string? Description, int QuestionCount);
public sealed record QuestionStartedDto(
    int QuestionIndex,
    int TotalQuestions,
    string Question,
    string[] Options,
    int TimeLimitSeconds,
    bool HasMultipleCorrectAnswers,
    int CorrectCount,
    string? MediaUrl,
    DateTimeOffset StartedAt
);
public sealed record QuestionEndedDto(
    int QuestionIndex,
    int[] CorrectIndices
);
