using QuizHub.Api;

var builder = WebApplication.CreateBuilder(args);

const string corsPolicy = "quiz-frontend";
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://localhost:4173"];

builder.Services.AddCors(options => options.AddPolicy(corsPolicy, policy =>
{
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
}));

builder.Services.AddSingleton<IQuizRepository, QuizRepository>();
builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());

var app = builder.Build();
app.UseCors(corsPolicy);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapGet("/api/quizzes", (IQuizRepository repo) => Results.Ok(repo.GetAvailableQuizzes()));
app.MapGet("/api/quizzes/{id}", (string id, IQuizRepository repo) =>
{
    var quiz = repo.GetQuiz(id);
    return quiz is not null ? Results.Ok(quiz) : Results.NotFound();
});
app.MapHub<global::QuizHub.Api.QuizHub>("/quizHub");
app.Run();
