namespace QuizHub.Api;

public sealed record LobbyCreated(string Code, string AdminConnectionId);
public sealed record PlayerDto(string Id, string Name, int Score = 0);
public sealed record LobbyDto(string Code, PlayerDto[] Players);
public sealed record AnswersProgress(int Answered, int Total);
public sealed record AnswerResult(bool IsCorrect, int Points);