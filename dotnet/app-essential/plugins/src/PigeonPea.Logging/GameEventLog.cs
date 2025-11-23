using Microsoft.Extensions.Logging;

namespace PigeonPea.Logging;

/// <summary>
/// Game event logging methods - source-generated for zero overhead.
/// Provides unified logging interface for all observability sinks.
/// </summary>
public static partial class GameEventLog
{
    // ===== Player Events (Event IDs: 1000-1999) =====

    /// <summary>
    /// Logs when a player moves from one position to another.
    /// </summary>
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Player moved from {FromX},{FromY} to {ToX},{ToY}")]
    public static partial void LogPlayerMove(
        this ILogger logger,
        int fromX, int fromY,
        int toX, int toY);

    /// <summary>
    /// Logs when a player attacks a target.
    /// </summary>
    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Player attacked {Target} with damage {Damage}")]
    public static partial void LogPlayerAttack(
        this ILogger logger,
        string target,
        int damage);

    /// <summary>
    /// Logs when a player takes damage.
    /// </summary>
    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Information,
        Message = "Player took {Damage} damage from {Source}, health: {Health}")]
    public static partial void LogPlayerDamage(
        this ILogger logger,
        int damage,
        string source,
        int health);

    /// <summary>
    /// Logs when a player heals.
    /// </summary>
    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Information,
        Message = "Player healed for {Amount}, health: {Health}")]
    public static partial void LogPlayerHeal(
        this ILogger logger,
        int amount,
        int health);

    /// <summary>
    /// Logs when a player dies.
    /// </summary>
    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Player died from {Cause}")]
    public static partial void LogPlayerDeath(
        this ILogger logger,
        string cause);

    /// <summary>
    /// Logs when a player respawns.
    /// </summary>
    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Information,
        Message = "Player respawned at {X},{Y}")]
    public static partial void LogPlayerRespawn(
        this ILogger logger,
        int x,
        int y);

    // ===== Entity Events (Event IDs: 2000-2999) =====

    /// <summary>
    /// Logs when an entity is spawned.
    /// </summary>
    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Information,
        Message = "Entity {EntityId} spawned at {X},{Y} with {ComponentCount} components")]
    public static partial void LogEntitySpawned(
        this ILogger logger,
        string entityId,
        int x, int y,
        int componentCount);

    /// <summary>
    /// Logs when a component is added to an entity.
    /// </summary>
    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Information,
        Message = "Component {ComponentType} added to entity {EntityId}")]
    public static partial void LogComponentAdded(
        this ILogger logger,
        string componentType,
        string entityId);

    /// <summary>
    /// Logs when a component is removed from an entity.
    /// </summary>
    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Information,
        Message = "Component {ComponentType} removed from entity {EntityId}")]
    public static partial void LogComponentRemoved(
        this ILogger logger,
        string componentType,
        string entityId);

    /// <summary>
    /// Logs when an entity is destroyed.
    /// </summary>
    [LoggerMessage(
        EventId = 2004,
        Level = LogLevel.Information,
        Message = "Entity {EntityId} destroyed")]
    public static partial void LogEntityDestroyed(
        this ILogger logger,
        string entityId);

    // ===== Map Events (Event IDs: 3000-3999) =====

    /// <summary>
    /// Logs when map generation starts.
    /// </summary>
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Map generation started with seed {Seed}")]
    public static partial void LogMapGenerationStart(
        this ILogger logger,
        int seed);

    /// <summary>
    /// Logs when map generation completes.
    /// </summary>
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Map generation completed: {TileCount} tiles, {RiverCount} rivers")]
    public static partial void LogMapGenerationComplete(
        this ILogger logger,
        int tileCount,
        int riverCount);

    /// <summary>
    /// Logs when a tile is modified.
    /// </summary>
    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Debug,
        Message = "Tile at {X},{Y} changed from {OldType} to {NewType}")]
    public static partial void LogTileModified(
        this ILogger logger,
        int x, int y,
        string oldType,
        string newType);

    // ===== AI Events (Event IDs: 4000-4999) =====

    /// <summary>
    /// Logs when an AI makes a decision.
    /// </summary>
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Information,
        Message = "AI {EntityId} decided {Decision} with score {Score}")]
    public static partial void LogAIDecision(
        this ILogger logger,
        string entityId,
        string decision,
        double score);

    /// <summary>
    /// Logs when an AI changes state.
    /// </summary>
    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "AI {EntityId} state changed from {OldState} to {NewState}")]
    public static partial void LogAIStateChange(
        this ILogger logger,
        string entityId,
        string oldState,
        string newState);

    /// <summary>
    /// Logs when an AI path is calculated.
    /// </summary>
    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Debug,
        Message = "AI {EntityId} calculated path from {StartX},{StartY} to {EndX},{EndY} with {StepCount} steps")]
    public static partial void LogAIPathCalculated(
        this ILogger logger,
        string entityId,
        int startX, int startY,
        int endX, int endY,
        int stepCount);

    // ===== Performance Events (Event IDs: 5000-5999) =====

    /// <summary>
    /// Logs when a frame takes too long.
    /// </summary>
    [LoggerMessage(
        EventId = 5001,
        Level = LogLevel.Warning,
        Message = "Frame took {FrameTimeMs}ms (target: 16.67ms)")]
    public static partial void LogSlowFrame(
        this ILogger logger,
        double frameTimeMs);

    /// <summary>
    /// Logs when a system update takes too long.
    /// </summary>
    [LoggerMessage(
        EventId = 5002,
        Level = LogLevel.Warning,
        Message = "System {SystemName} update took {UpdateTimeMs}ms")]
    public static partial void LogSlowSystemUpdate(
        this ILogger logger,
        string systemName,
        double updateTimeMs);

    /// <summary>
    /// Logs garbage collection events.
    /// </summary>
    [LoggerMessage(
        EventId = 5003,
        Level = LogLevel.Information,
        Message = "GC {Generation}: {PauseTimeMs}ms pause, {MemoryBeforeMB}MB → {MemoryAfterMB}MB")]
    public static partial void LogGarbageCollection(
        this ILogger logger,
        int generation,
        double pauseTimeMs,
        double memoryBeforeMB,
        double memoryAfterMB);

    // ===== Input Events (Event IDs: 6000-6999) =====

    /// <summary>
    /// Logs keyboard input.
    /// </summary>
    [LoggerMessage(
        EventId = 6001,
        Level = LogLevel.Debug,
        Message = "Key {Key} {Action}")]
    public static partial void LogKeyInput(
        this ILogger logger,
        string key,
        string action);

    /// <summary>
    /// Logs mouse input.
    /// </summary>
    [LoggerMessage(
        EventId = 6002,
        Level = LogLevel.Debug,
        Message = "Mouse {Button} {Action} at {X},{Y}")]
    public static partial void LogMouseInput(
        this ILogger logger,
        string button,
        string action,
        int x,
        int y);

    /// <summary>
    /// Logs gamepad input.
    /// </summary>
    [LoggerMessage(
        EventId = 6003,
        Level = LogLevel.Debug,
        Message = "Gamepad {Button} {Action} on controller {ControllerId}")]
    public static partial void LogGamepadInput(
        this ILogger logger,
        string button,
        string action,
        int controllerId);

    // ===== Audio Events (Event IDs: 7000-7999) =====

    /// <summary>
    /// Logs when a sound is played.
    /// </summary>
    [LoggerMessage(
        EventId = 7001,
        Level = LogLevel.Debug,
        Message = "Sound {SoundName} played at volume {Volume}")]
    public static partial void LogSoundPlayed(
        this ILogger logger,
        string soundName,
        float volume);

    /// <summary>
    /// Logs when music track changes.
    /// </summary>
    [LoggerMessage(
        EventId = 7002,
        Level = LogLevel.Information,
        Message = "Music changed from {OldTrack} to {NewTrack}")]
    public static partial void LogMusicChanged(
        this ILogger logger,
        string? oldTrack,
        string newTrack);

    // ===== Network Events (Event IDs: 8000-8999) =====

    /// <summary>
    /// Logs when a network connection is established.
    /// </summary>
    [LoggerMessage(
        EventId = 8001,
        Level = LogLevel.Information,
        Message = "Connected to {ServerAddress}:{Port}")]
    public static partial void LogNetworkConnected(
        this ILogger logger,
        string serverAddress,
        int port);

    /// <summary>
    /// Logs when a network connection is lost.
    /// </summary>
    [LoggerMessage(
        EventId = 8002,
        Level = LogLevel.Warning,
        Message = "Disconnected from {ServerAddress}:{Port} - {Reason}")]
    public static partial void LogNetworkDisconnected(
        this ILogger logger,
        string serverAddress,
        int port,
        string reason);

    /// <summary>
    /// Logs network packet transmission.
    /// </summary>
    [LoggerMessage(
        EventId = 8003,
        Level = LogLevel.Debug,
        Message = "Packet {PacketType} sent to {ClientId}, size: {Size} bytes")]
    public static partial void LogNetworkPacketSent(
        this ILogger logger,
        string packetType,
        string clientId,
        int size);

    // ===== Error Events (Event IDs: 9000-9999) =====

    /// <summary>
    /// Logs general game errors.
    /// </summary>
    [LoggerMessage(
        EventId = 9001,
        Level = LogLevel.Error,
        Message = "Game error in {System}: {ErrorMessage}")]
    public static partial void LogGameError(
        this ILogger logger,
        string system,
        string errorMessage,
        Exception? exception);

    /// <summary>
    /// Logs validation errors.
    /// </summary>
    [LoggerMessage(
        EventId = 9002,
        Level = LogLevel.Warning,
        Message = "Validation failed in {Component}: {ValidationMessage}")]
    public static partial void LogValidationError(
        this ILogger logger,
        string component,
        string validationMessage);

    /// <summary>
    /// Logs configuration errors.
    /// </summary>
    [LoggerMessage(
        EventId = 9003,
        Level = LogLevel.Error,
        Message = "Configuration error: {ConfigError}")]
    public static partial void LogConfigurationError(
        this ILogger logger,
        string configError);

    // ===== System Events (Event IDs: 10000-10999) =====

    /// <summary>
    /// Logs when the game starts.
    /// </summary>
    [LoggerMessage(
        EventId = 10001,
        Level = LogLevel.Information,
        Message = "Game started with version {Version}")]
    public static partial void LogGameStarted(
        this ILogger logger,
        string version);

    /// <summary>
    /// Logs when the game shuts down.
    /// </summary>
    [LoggerMessage(
        EventId = 10002,
        Level = LogLevel.Information,
        Message = "Game shutting down after {UptimeMinutes} minutes")]
    public static partial void LogGameShutdown(
        this ILogger logger,
        double uptimeMinutes);

    /// <summary>
    /// Logs when a plugin is loaded.
    /// </summary>
    [LoggerMessage(
        EventId = 10003,
        Level = LogLevel.Information,
        Message = "Plugin {PluginName} v{Version} loaded")]
    public static partial void LogPluginLoaded(
        this ILogger logger,
        string pluginName,
        string version);

    /// <summary>
    /// Logs when a plugin is unloaded.
    /// </summary>
    [LoggerMessage(
        EventId = 10004,
        Level = LogLevel.Information,
        Message = "Plugin {PluginName} unloaded")]
    public static partial void LogPluginUnloaded(
        this ILogger logger,
        string pluginName);
}
