namespace QuizHub.Api;

internal sealed record Answer(int SelectedOption, bool IsCorrect, int Points);