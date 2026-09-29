using System.Text.Json;

namespace QuizHub.Api;

public interface IQuizRepository
{
    IReadOnlyList<QuizSummaryDto> GetAvailableQuizzes();
    Quiz? GetQuiz(string? id);
    string QuizzesDirectory { get; }
}

public sealed class QuizRepository : IQuizRepository
{
    private readonly ILogger<QuizRepository> _logger;
    private readonly string _quizzesDirectory;

    public string QuizzesDirectory => _quizzesDirectory;

    public QuizRepository(IWebHostEnvironment environment, IConfiguration configuration, ILogger<QuizRepository> logger)
    {
        _logger = logger;
        _quizzesDirectory = ResolveQuizzesDirectory(environment, configuration);
        _logger.LogInformation("Quizzes directory resolved to: {Directory}", _quizzesDirectory);
    }

    private static string ResolveQuizzesDirectory(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configured = configuration["QuizDirectory"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            return Path.GetFullPath(configured);

        string[] candidates =
        [
            Path.Combine(AppContext.BaseDirectory, "quizzes"),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", "quizzes")),
            Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "quizzes")),
            Path.Combine(environment.ContentRootPath, "quizzes"),
            Path.Combine(Directory.GetCurrentDirectory(), "quizzes"),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "quizzes"))
        ];

        foreach (var candidate in candidates)
        {
            if (Directory.Exists(candidate))
                return candidate;
        }

        // If none exists yet, create one under ContentRootPath/quizzes
        var defaultPath = Path.Combine(environment.ContentRootPath, "quizzes");
        Directory.CreateDirectory(defaultPath);
        return defaultPath;
    }

    public IReadOnlyList<QuizSummaryDto> GetAvailableQuizzes()
    {
        var quizzes = LoadAllQuizzes();
        return quizzes.Select(q => new QuizSummaryDto(q.Id, q.Name, q.Description, q.Questions.Count)).ToList();
    }

    public Quiz? GetQuiz(string? id)
    {
        var quizzes = LoadAllQuizzes();
        if (quizzes.Count == 0)
            return null;

        if (string.IsNullOrWhiteSpace(id))
            return quizzes[0];

        return quizzes.FirstOrDefault(q => q.Id.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? quizzes.FirstOrDefault(q => q.Name.Equals(id, StringComparison.OrdinalIgnoreCase))
            ?? quizzes[0];
    }

    private List<Quiz> LoadAllQuizzes()
    {
        var list = new List<Quiz>();
        if (!Directory.Exists(_quizzesDirectory))
            return list;

        var files = Directory.GetFiles(_quizzesDirectory, "*.json", SearchOption.TopDirectoryOnly);
        foreach (var file in files)
        {
            try
            {
                var quiz = ParseQuizFile(file);
                if (quiz is not null && quiz.Questions.Count > 0)
                {
                    list.Add(quiz);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse quiz file: {File}", file);
            }
        }

        return list;
    }

    private static Quiz? ParseQuizFile(string filePath)
    {
        var jsonText = File.ReadAllText(filePath);
        using var document = JsonDocument.Parse(jsonText, new JsonDocumentOptions { AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
        var root = document.RootElement;

        var id = Path.GetFileNameWithoutExtension(filePath);
        var name = GetStringProperty(root, "name", "quizName", "title") ?? id;
        var description = GetStringProperty(root, "description", "beschreibung");

        var questions = new List<QuizQuestion>();
        if (TryGetProperty(root, out var questionsElement, "questions", "fragen") && questionsElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var qEl in questionsElement.EnumerateArray())
            {
                var questionText = GetStringProperty(qEl, "question", "frage", "title");
                if (string.IsNullOrWhiteSpace(questionText))
                    continue;

                var mediaUrl = GetStringProperty(qEl, "mediaUrl", "bild", "video");

                var options = new List<QuizOption>();
                if (TryGetProperty(qEl, out var optionsElement, "options", "answers", "antworten") && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var optEl in optionsElement.EnumerateArray())
                    {
                        var optText = GetStringProperty(optEl, "text", "antwort", "answer", "option") ?? "";
                        var isCorrect = GetBoolProperty(optEl, false, "isCorrect", "correct", "richtig");
                        options.Add(new QuizOption(optText, isCorrect));
                    }
                }

                if (options.Count >= 2)
                {
                    questions.Add(new QuizQuestion(questionText, options, mediaUrl));
                }
            }
        }

        return new Quiz(id, name, description, questions);
    }

    private static bool TryGetProperty(JsonElement element, out JsonElement property, params string[] names)
    {
        foreach (var name in names)
        {
            if (element.TryGetProperty(name, out property))
                return true;

            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    property = prop.Value;
                    return true;
                }
            }
        }
        property = default;
        return false;
    }

    private static string? GetStringProperty(JsonElement element, params string[] names)
    {
        if (TryGetProperty(element, out var prop, names))
        {
            if (prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
        }
        return null;
    }

    private static int GetIntProperty(JsonElement element, int defaultValue, params string[] names)
    {
        if (TryGetProperty(element, out var prop, names))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var value))
                return value;
        }
        return defaultValue;
    }

    private static bool GetBoolProperty(JsonElement element, bool defaultValue, params string[] names)
    {
        if (TryGetProperty(element, out var prop, names))
        {
            if (prop.ValueKind is JsonValueKind.True or JsonValueKind.False)
                return prop.GetBoolean();
            if (prop.ValueKind == JsonValueKind.String && bool.TryParse(prop.GetString(), out var parsed))
                return parsed;
        }
        return defaultValue;
    }
}
