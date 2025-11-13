using System.Text.Json.Serialization;

namespace PigeonPea.DevTools.Protocol;

/// <summary>
/// Base command sent from dev tools client to game server.
/// </summary>
public class DevCommand
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "command";

    [JsonPropertyName("cmd")]
    public string Cmd { get; set; } = string.Empty;

    [JsonPropertyName("args")]
    public Dictionary<string, object>? Args { get; set; }
}

/// <summary>
/// Spawn entity command.
/// </summary>
public class SpawnCommand
{
    [JsonPropertyName("entity")]
    public string Entity { get; set; } = string.Empty;

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}

/// <summary>
/// Teleport entity command.
/// </summary>
public class TeleportCommand
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; } // "player" or null for player

    [JsonPropertyName("x")]
    public int X { get; set; }

    [JsonPropertyName("y")]
    public int Y { get; set; }
}

/// <summary>
/// Query entities command.
/// </summary>
public class QueryCommand
{
    [JsonPropertyName("filter")]
    public string? Filter { get; set; } // e.g., "health < 50", "type:enemy", etc.
}

/// <summary>
/// Give item to player command.
/// </summary>
public class GiveItemCommand
{
    [JsonPropertyName("item")]
    public string Item { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; } = 1;
}

/// <summary>
/// Heal entity command.
/// </summary>
public class HealCommand
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; } // "player" or null for player

    [JsonPropertyName("amount")]
    public int Amount { get; set; }
}

/// <summary>
/// Kill entity command.
/// </summary>
public class KillCommand
{
    [JsonPropertyName("entity")]
    public string? Entity { get; set; } // "all", "enemies", or null for nearest enemy
}
