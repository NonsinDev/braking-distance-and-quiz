namespace QuizHub.Api;

internal sealed record Answer(int[] SelectedOptions, bool IsCorrect, int Points, bool IsPartial = false);
