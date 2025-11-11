using System.Collections.Generic;

namespace PigeonPea.Contracts.Plugin;

/// <summary>
/// Cross-ALC service registry for publishing and resolving services by priority.
/// </summary>
public interface IRegistry
{
    /// <summary>
    /// Register a service implementation with accompanying metadata.
    /// </summary>
    void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class;

    /// <summary>
    /// Register a service implementation with a priority (higher preferred).
    /// </summary>
    void Register<TService>(TService implementation, int priority = 100) where TService : class;

    /// <summary>
    /// Resolve a service according to the selection mode (default highest-priority).
    /// </summary>
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;

    /// <summary>
    /// Get all registered implementations for the given service type.
    /// </summary>
    IEnumerable<TService> GetAll<TService>() where TService : class;

    /// <summary>
    /// Whether any implementation is registered for the given service type.
    /// </summary>
    bool IsRegistered<TService>() where TService : class;

    /// <summary>
    /// Unregister a specific implementation instance.
    /// </summary>
    bool Unregister<TService>(TService implementation) where TService : class;
}

/// <summary>
/// Selection behavior when resolving services.
/// </summary>
public enum SelectionMode
{
    /// <summary>
    /// Exactly one implementation must be registered; otherwise throw.
    /// </summary>
    One,

    /// <summary>
    /// Return the highest-priority implementation (default behavior).
    /// </summary>
    HighestPriority,

    /// <summary>
    /// Invalid for single resolution; use GetAll() instead.
    /// </summary>
    All
}
