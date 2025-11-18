using System;

namespace PigeonPea.Contracts.Plugin.Attributes;

/// <summary>
/// Marks a partial class for automatic proxy service generation.
/// The source generator will implement all interface members by delegating to IRegistry.
/// </summary>
/// <remarks>
/// The partial class must have an IRegistry _registry field.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RealizeServiceAttribute : Attribute
{
    /// <summary>
    /// Gets the service interface type to implement.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RealizeServiceAttribute"/> class.
    /// </summary>
    /// <param name="serviceType">The service interface type to implement.</param>
    public RealizeServiceAttribute(Type serviceType)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
    }
}
