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
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <param name="implementation">The concrete implementation instance.</param>
    /// <param name="metadata">Registration metadata, including priority and optional naming.</param>
    void Register<TService>(TService implementation, ServiceMetadata metadata) where TService : class;

    /// <summary>
    /// Register a service implementation with a priority (higher preferred).
    /// </summary>
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <param name="implementation">The concrete implementation instance.</param>
    /// <param name="priority">Priority used when selecting a single implementation.</param>
    void Register<TService>(TService implementation, int priority = 100) where TService : class;

    /// <summary>
    /// Resolve a service according to the selection mode (default highest-priority).
    /// </summary>
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <param name="mode">
    /// Selection behavior:
    /// <list type="bullet">
    /// <item><description><see cref="SelectionMode.HighestPriority"/> (default) returns the highest-priority implementation; throws if none registered.</description></item>
    /// <item><description><see cref="SelectionMode.One"/> requires exactly one implementation; throws if zero or multiple are registered.</description></item>
    /// <item><description><see cref="SelectionMode.All"/> is invalid for single resolution and should throw. Use <see cref="GetAll{TService}()"/> instead.</description></item>
    /// </list>
    /// </param>
    /// <returns>The selected implementation instance.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Thrown when no suitable implementation exists or when <see cref="SelectionMode.One"/> is used and multiple implementations are registered.
    /// </exception>
    TService Get<TService>(SelectionMode mode = SelectionMode.HighestPriority) where TService : class;

    /// <summary>
    /// Get all registered implementations for the given service type.
    /// </summary>
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <returns>All registered implementations, typically ordered by descending priority.</returns>
    IEnumerable<TService> GetAll<TService>() where TService : class;

    /// <summary>
    /// Whether any implementation is registered for the given service type.
    /// </summary>
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <returns><c>true</c> if at least one implementation is registered; otherwise <c>false</c>.</returns>
    bool IsRegistered<TService>() where TService : class;

    /// <summary>
    /// Unregister a specific implementation instance.
    /// </summary>
    /// <typeparam name="TService">The service abstraction type.</typeparam>
    /// <param name="implementation">The instance to remove.</param>
    /// <returns><c>true</c> if the instance was found and removed; otherwise <c>false</c>.</returns>
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
