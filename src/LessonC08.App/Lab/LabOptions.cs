namespace LessonC08.App.Lab;

/// <summary>Server-side configuration read from .env / environment variables. Never sent to the browser.</summary>
public sealed record LabOptions(string ApiKey, string AgentId, string EnvironmentId, int Port)
{
    public bool Configured => new[] { ApiKey, AgentId, EnvironmentId }.All(IsSet);

    public static bool IsSet(string? value) => !string.IsNullOrWhiteSpace(value) && !value.StartsWith("your_");

    public static LabOptions FromEnvironment() => new(
        Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") ?? "",
        Environment.GetEnvironmentVariable("CLAUDE_MANAGED_AGENT_ID") ?? "",
        Environment.GetEnvironmentVariable("CLAUDE_MANAGED_ENVIRONMENT_ID") ?? "",
        int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port) ? port : 3008);
}
