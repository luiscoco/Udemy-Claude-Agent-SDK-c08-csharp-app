using System.Text.Json;
using Anthropic;
using Anthropic.Models.Beta.Sessions;
using Anthropic.Models.Beta.Sessions.Events;

namespace LessonC08.App.Lab;

public abstract record RunUpdate
{
    public sealed record SessionStarted(string SessionId) : RunUpdate;
    public sealed record Routed(Sink Sink, JsonElement Event) : RunUpdate;
    public sealed record Stopped(string Reason) : RunUpdate;
    public sealed record Failed(string Message) : RunUpdate;
}

/// <summary>Creates a live session, observes its event stream and routes each event. Credentials stay on the server.</summary>
public sealed class SessionRunner(LabOptions options)
{
    public async Task RunAsync(string prompt, Func<RunUpdate, Task> send, CancellationToken cancellationToken)
    {
        try
        {
            AnthropicClient client = new() { ApiKey = options.ApiKey };
            var session = await client.Beta.Sessions.Create(new SessionCreateParams
            {
                Agent = options.AgentId,
                EnvironmentID = options.EnvironmentId,
                // Amount is in minor units: "50" is fifty cents.
                Budget = new BetaManagedAgentsBudgetLimit { MaxListCost = new() { Amount = "50", Currency = "USD" } },
            }, cancellationToken);
            await send(new RunUpdate.SessionStarted(session.ID));

            // Open the stream before sending the kickoff message so no early event is missed.
            await using var stream = client.Beta.Sessions.Events
                .StreamStreaming(session.ID, new EventStreamParams(), cancellationToken)
                .GetAsyncEnumerator(cancellationToken);
            var next = stream.MoveNextAsync();

            await client.Beta.Sessions.Events.Send(session.ID, new EventSendParams
            {
                Events =
                [
                    new BetaManagedAgentsUserMessageEventParams
                    {
                        Content = [new BetaManagedAgentsTextBlock { Text = prompt }],
                    },
                ],
            }, cancellationToken);

            var router = new EventRouter();
            while (await next)
            {
                var evt = JsonSerializer.SerializeToElement(stream.Current);
                await router.RouteAsync(evt, (sink, e) => send(new RunUpdate.Routed(sink, e)));

                switch (EventRouter.ReadString(evt, "type"))
                {
                    case "session.status_idle":
                        var reason = evt.TryGetProperty("stop_reason", out var stop) ? EventRouter.ReadString(stop, "type") : null;
                        await send(new RunUpdate.Stopped(reason ?? "idle"));
                        return;
                    case "session.status_terminated":
                        await send(new RunUpdate.Stopped("terminated"));
                        return;
                    case "session.error":
                        await send(new RunUpdate.Failed("Provider reported a session error."));
                        return;
                }
                next = stream.MoveNextAsync();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The page closed. Closing the observer does not stop remote work.
        }
        catch (Exception error)
        {
            await send(new RunUpdate.Failed(error.Message));
        }
    }
}
