using System;
using PigeonPea.Contracts.DependencyInjection;
using Splat;

namespace PigeonPea.PluginSystem;

public sealed class SplatAppServiceLocator : IAppServiceLocator
{
    private readonly IMutableDependencyResolver _mutable;
    private readonly IDependencyResolver _readonly;
    private readonly SplatAppServiceLocator? _parent;

    private SplatAppServiceLocator(
        IMutableDependencyResolver mutable,
        IDependencyResolver @readonly,
        SplatAppServiceLocator? parent)
    {
        _mutable = mutable ?? throw new ArgumentNullException(nameof(mutable));
        _readonly = @readonly ?? throw new ArgumentNullException(nameof(@readonly));
        _parent = parent;
    }

    public static SplatAppServiceLocator CreateRoot()
    {
        var resolver = new ModernDependencyResolver();
        return new SplatAppServiceLocator(resolver, resolver, null);
    }

    public T Get<T>()
    {
        var value = Get(typeof(T));
        if (value is null)
        {
            throw new InvalidOperationException($"Service of type '{typeof(T)}' is not registered in this locator.");
        }

        return (T)value;
    }

    public object? Get(Type serviceType)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));

        var value = _readonly.GetService(serviceType);
        if (value is not null)
        {
            return value;
        }

        return _parent?.Get(serviceType);
    }

    public void Register<T>(Func<T> factory)
    {
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        // IMutableDependencyResolver.Register expects a Func<object?>, so wrap the generic factory.
        _mutable.Register(() => (object?)factory(), typeof(T));
    }

    public void Register(Type serviceType, Func<object> factory)
    {
        if (serviceType is null) throw new ArgumentNullException(nameof(serviceType));
        if (factory is null) throw new ArgumentNullException(nameof(factory));
        _mutable.Register(factory, serviceType);
    }

    public IAppServiceLocator CreateScope()
    {
        var resolver = new ModernDependencyResolver();
        return new SplatAppServiceLocator(resolver, resolver, this);
    }
}
