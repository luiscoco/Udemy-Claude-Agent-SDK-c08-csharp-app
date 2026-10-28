using System.Text.Json;

namespace LessonC08.App.Lab;

public enum Sink { Preview, Text, Action, Usage, Lifecycle }

/// <summary>Routes raw session events to sinks. One instance per run: deduplication is scoped to that run.</summary>
public sealed class EventRouter
{
    private readonly HashSet<string> _seen = [];

    public async Task RouteAsync(JsonElement evt, Func<Sink, JsonElement, Task> dispatch)
    {
        var type = ReadString(evt, "type");
        var id = ReadString(evt, "id");

        // Previews can share a target ID, so they bypass persisted-ID deduplication.
        if (type is "event_start" or "event_delta") { await dispatch(Sink.Preview, evt); return; }
        if (id is not null && _seen.Contains(id)) return;

        var sink = Sink.Lifecycle;
        if (type == "agent.message") sink = Sink.Text;
        else if (type == "agent.custom_tool_use" || ReadString(evt, "evaluated_permission") == "ask") sink = Sink.Action;
        else if (type == "span.model_request_end") sink = Sink.Usage;

        await dispatch(sink, evt);
        // Mark seen only after dispatch succeeds, so a failed dispatch can be retried.
        if (id is not null) _seen.Add(id);
    }

    public static string? ReadString(JsonElement evt, string name) =>
        evt.ValueKind == JsonValueKind.Object && evt.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
}
