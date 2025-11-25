namespace PigeonPea.Platform.Contracts.Core;

/// <summary>
/// Service registry for cross-ALC plugin communication.
/// Uses runtime type matching (not compile-time references) to support plugin isolation.
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Register a service implementation with metadata (priority, name, version).
    /// </summary>
    void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class;

    /// <summary>
    /// Register a service implementation with priority (shorthand for common case).
    /// </summary>
    void Register<TService>(TService implementation, int priority = 100) where TService : class;

    /// <summary>
    /// Get a single service implementation using specified selection mode.
    /// </summary>
    /// <exception cref="InvalidOperationException">If SelectionMode.One and multiple or zero implementations exist.</exception>
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;

    /// <summary>
    /// Get all registered implementations of a service.
    /// </summary>
    IEnumerable<TService> GetAll<TService>() where TService : class;

    /// <summary>
    /// Check if any implementation is registered for a service.
    /// </summary>
    bool IsRegistered<TService>() where TService : class;

    /// <summary>
    /// Unregister a specific service implementation (used during plugin unload).
    /// </summary>
    bool Unregister<TService>(TService implementation) where TService : class;
}

/// <summary>
/// Selection mode for Get&lt;TService&gt; when multiple implementations exist.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Exactly one implementation required. Throws if zero or multiple exist.
    /// </summary>
    One,

    /// <summary>
    /// Return the implementation with highest priority. Default behavior.
    /// </summary>
    HighestPriority,

    /// <summary>
    /// Throw exception; caller should use GetAll() instead.
    /// </summary>
    All
}

/// <summary>
/// Metadata for service registration (priority, versioning, naming).
/// </summary>
public class ServiceMetadata
{
    /// <summary>
    /// Priority for conflict resolution. Higher = preferred. Default: 100.
    /// Framework services: 1000+, Game plugins: 100-500, UI plugins: 50-99.
    /// </summary>
    public int Priority { get; set; } = 100;

    /// <summary>
    /// Optional name for multi-named services (e.g., "TerminalUI", "SadConsole").
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Optional version for tracking service compatibility.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Plugin ID that registered this service (set by registry automatically).
    /// </summary>
    public string? PluginId { get; set; }
}
