namespace QuizHub.Api;

public sealed record QuizOption(string Text, bool IsCorrect);

public sealed record QuizQuestion(string Question, List<QuizOption> Options, string? MediaUrl = null)
{
    public int[] CorrectIndices => Options
        .Select((option, index) => (option, index))
        .Where(item => item.option.IsCorrect)
        .Select(item => item.index)
        .ToArray();

    public bool HasMultipleCorrectAnswers => CorrectIndices.Length > 1;
}

public sealed record Quiz(string Id, string Name, string? Description, List<QuizQuestion> Questions);
