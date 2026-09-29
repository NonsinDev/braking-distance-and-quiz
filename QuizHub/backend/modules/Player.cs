namespace QuizHub.Api;

internal sealed class Player(string id, string name)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public int Score { get; set; }
}