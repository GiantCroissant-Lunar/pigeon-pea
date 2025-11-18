using System;
using Microsoft.Extensions.DependencyInjection;

namespace PigeonPea.Contracts.DependencyInjection;

public interface IAppServiceRoot : IServiceProvider, IAsyncDisposable
{
    IAppServiceScope CreateScope();
    IAppServiceLocator Locator { get; }
}

public interface IAppServiceScope : IServiceProvider, IAsyncDisposable
{
    IAppServiceLocator Locator { get; }
}

public interface IAppServiceRootFactory
{
    IAppServiceRoot Build(IServiceCollection services);
}

public interface IAppServiceLocator
{
    T Get<T>();
    object? Get(Type serviceType);

    void Register<T>(Func<T> factory);
    void Register(Type serviceType, Func<object> factory);

    IAppServiceLocator CreateScope();
}
