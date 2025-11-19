using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PigeonPea.Contracts.DependencyInjection;

namespace PigeonPea.PluginSystem;

public sealed class MsDiAppServiceRoot : IAppServiceRoot
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceScopeFactory _scopeFactory;

    public MsDiAppServiceRoot(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        Locator = SplatAppServiceLocator.CreateRoot();
    }

    public IAppServiceLocator Locator { get; }

    public object? GetService(Type serviceType)
    {
        return _serviceProvider.GetService(serviceType);
    }

    public IAppServiceScope CreateScope()
    {
        var scope = _scopeFactory.CreateScope();
        var locator = (SplatAppServiceLocator)Locator.CreateScope();
        return new MsDiAppServiceScope(scope, locator);
    }

    public ValueTask DisposeAsync()
    {
        if (_serviceProvider is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}

public sealed class MsDiAppServiceScope : IAppServiceScope
{
    private readonly IServiceScope _scope;

    public MsDiAppServiceScope(IServiceScope scope, IAppServiceLocator locator)
    {
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        Locator = locator ?? throw new ArgumentNullException(nameof(locator));
    }

    public IAppServiceLocator Locator { get; }

    public object? GetService(Type serviceType)
    {
        return _scope.ServiceProvider.GetService(serviceType);
    }

    public ValueTask DisposeAsync()
    {
        if (_scope is IAsyncDisposable asyncDisposable)
        {
            return asyncDisposable.DisposeAsync();
        }

        _scope.Dispose();
        return ValueTask.CompletedTask;
    }
}

public sealed class MsDiAppServiceRootFactory : IAppServiceRootFactory
{
    public IAppServiceRoot Build(IServiceCollection services)
    {
        if (services is null) throw new ArgumentNullException(nameof(services));

        var provider = services.BuildServiceProvider();
        return new MsDiAppServiceRoot(provider);
    }
}
