using System.Text.Json.Serialization;

namespace PigeonPea.DevTools.Protocol;

/// <summary>
/// Base event sent from game server to dev tools clients.
/// </summary>
public class DevEvent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "event";

    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    [JsonPropertyName("data")]
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// Entity created event.
/// </summary>
public class EntityCreatedEvent : DevEvent
{
    public EntityCreatedEvent()
    {
        Event = "entity_created";
    }

    [JsonPropertyName("entityId")]
    public int EntityId { get; set; }

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}

/// <summary>
/// Entity moved event.
/// </summary>
public class EntityMovedEvent : DevEvent
{
    public EntityMovedEvent()
    {
        Event = "entity_moved";
    }

    [JsonPropertyName("entityId")]
    public int EntityId { get; set; }

    [JsonPropertyName("fromX")]
    public int FromX { get; set; }

    [JsonPropertyName("fromY")]
    public int FromY { get; set; }

    [JsonPropertyName("toX")]
    public int ToX { get; set; }

    [JsonPropertyName("toY")]
    public int ToY { get; set; }
}

/// <summary>
/// Entity died event.
/// </summary>
public class EntityDiedEvent : DevEvent
{
    public EntityDiedEvent()
    {
        Event = "entity_died";
    }

    [JsonPropertyName("entityId")]
    public int EntityId { get; set; }

    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;
}

/// <summary>
/// Command result response.
/// </summary>
public class CommandResultEvent : DevEvent
{
    public CommandResultEvent()
    {
        Event = "command_result";
    }

    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public object? Result { get; set; }
}
