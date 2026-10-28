using Anthropic;
using Anthropic.Models.Beta.Agents;
using Anthropic.Models.Beta.Environments;

namespace LessonC08.App.Lab;

/// <summary>Optional one-time provisioning (<c>dotnet run -- setup</c>). Reuses IDs already present in .env.</summary>
public static class SetupResources
{
    public static async Task RunAsync()
    {
        var apiKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        if (!LabOptions.IsSet(apiKey)) throw new InvalidOperationException("Set your own ANTHROPIC_API_KEY in .env first.");

        AnthropicClient client = new() { ApiKey = apiKey, MaxRetries = 0 };

        if (!LabOptions.IsSet(Environment.GetEnvironmentVariable("CLAUDE_MANAGED_ENVIRONMENT_ID")))
        {
            var environment = await client.Beta.Environments.Create(new EnvironmentCreateParams
            {
                Name = "C08 student environment",
                Config = new BetaCloudConfigParams { Networking = new BetaLimitedNetworkParams() },
            });
            // Save immediately so a later agent failure does not lose the environment ID.
            DotEnv.Save("CLAUDE_MANAGED_ENVIRONMENT_ID", environment.ID);
        }

        if (!LabOptions.IsSet(Environment.GetEnvironmentVariable("CLAUDE_MANAGED_AGENT_ID")))
        {
            var model = Environment.GetEnvironmentVariable("CLAUDE_MODEL");
            var agent = await client.Beta.Agents.Create(new AgentCreateParams
            {
                Name = "C08 text-only student agent",
                Model = new BetaManagedAgentsModelConfigParams { ID = string.IsNullOrWhiteSpace(model) ? "claude-opus-5-5" : model },
                System = "Answer the student's question clearly and briefly.",
                Tools = [],
                McpServers = [],
                Skills = [],
            });
            DotEnv.Save("CLAUDE_MANAGED_AGENT_ID", agent.ID);
        }

        Console.WriteLine("Prerequisite IDs are saved in your local .env. Existing configured IDs were reused.");
    }
}
