using System.Text.Json;
using LessonC08.App.Lab;

namespace LessonC08.Tests;

public class EventRouterTests
{
    private static JsonElement Event(string type, string id) =>
        JsonSerializer.SerializeToElement(new { type, id });

    [Fact]
    public async Task PreviewsSharingAnIdAreRetainedWhilePersistedRecordsDeduplicate()
    {
        var route = new EventRouter();
        var seen = new List<Sink>();
        Task Sink(Sink kind, JsonElement _) { seen.Add(kind); return Task.CompletedTask; }

        await route.RouteAsync(Event("event_delta", "same"), Sink);
        await route.RouteAsync(Event("event_delta", "same"), Sink);
        await route.RouteAsync(Event("agent.message", "same"), Sink);
        await route.RouteAsync(Event("agent.message", "same"), Sink);

        Assert.Equal([LessonC08.App.Lab.Sink.Preview, LessonC08.App.Lab.Sink.Preview, LessonC08.App.Lab.Sink.Text], seen);
    }

    [Fact]
    public async Task FailedActionsCanBeRetriedAndUsageHasItsOwnRoute()
    {
        var route = new EventRouter();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            route.RouteAsync(Event("agent.custom_tool_use", "one"), (_, _) => throw new InvalidOperationException("failed")));

        var calls = 0;
        await route.RouteAsync(Event("agent.custom_tool_use", "one"), (_, _) => { calls++; return Task.CompletedTask; });
        await route.RouteAsync(Event("span.model_request_end", "two"), (kind, _) =>
        {
            Assert.Equal(Sink.Usage, kind);
            return Task.CompletedTask;
        });
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task AskPermissionRoutesToAction()
    {
        var route = new EventRouter();
        Sink? routed = null;
        var evt = JsonSerializer.SerializeToElement(new { type = "agent.tool_use", id = "t1", evaluated_permission = "ask" });
        await route.RouteAsync(evt, (kind, _) => { routed = kind; return Task.CompletedTask; });
        Assert.Equal(Sink.Action, routed);
    }
}
