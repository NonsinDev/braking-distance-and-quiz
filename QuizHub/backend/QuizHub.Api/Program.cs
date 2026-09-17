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

builder.Services.AddSignalR(options => options.EnableDetailedErrors = builder.Environment.IsDevelopment());

var app = builder.Build();
app.UseCors(corsPolicy);
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapHub<global::QuizHub.Api.QuizHub>("/quizHub");
app.Run();
