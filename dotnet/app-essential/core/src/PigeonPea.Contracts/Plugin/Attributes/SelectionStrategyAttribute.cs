using System;

namespace PigeonPea.Contracts.Plugin.Attributes;

/// <summary>
/// Specifies the selection strategy to use when retrieving services from IRegistry.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class SelectionStrategyAttribute : Attribute
{
    /// <summary>
    /// Gets the selection mode to use.
    /// </summary>
    public SelectionMode Mode { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SelectionStrategyAttribute"/> class.
    /// </summary>
    /// <param name="mode">The selection mode to use when retrieving services.</param>
    public SelectionStrategyAttribute(SelectionMode mode)
    {
        Mode = mode;
    }
}
