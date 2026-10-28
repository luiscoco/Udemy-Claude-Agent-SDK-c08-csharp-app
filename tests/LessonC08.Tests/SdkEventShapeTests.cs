using System.Text.Json;
using Anthropic.Models.Beta.Sessions.Events;
using LessonC08.App.Lab;

namespace LessonC08.Tests;

public class SdkEventShapeTests
{
    [Fact]
    public void StreamEventsSerializeBackToWireJson()
    {
        const string wire = """
            {"type":"session.status_idle","id":"sevt_1","processed_at":"2026-10-28T10:00:00Z","stop_reason":{"type":"end_turn"}}
            """;
        var parsed = JsonSerializer.Deserialize<BetaManagedAgentsStreamSessionEvents>(wire)!;
        var evt = JsonSerializer.SerializeToElement(parsed);

        Assert.Equal("session.status_idle", EventRouter.ReadString(evt, "type"));
        Assert.Equal("sevt_1", EventRouter.ReadString(evt, "id"));
        Assert.Equal("end_turn", EventRouter.ReadString(evt.GetProperty("stop_reason"), "type"));
    }
}
