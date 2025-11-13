using System;
using System.Linq;
using FluentAssertions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.PluginSystem;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class ServiceRegistryTests
{
    private interface IFoo { int V { get; } }
    private class FooA : IFoo { public int V { get; init; } = 1; }
    private class FooB : IFoo { public int V { get; init; } = 2; }

    [Fact]
    public void Register_And_Get_HighestPriority_Works()
    {
        var reg = new ServiceRegistry();
        var a = new FooA();
        var b = new FooB();

        reg.Register<IFoo>(a, priority: 100);
        reg.Register<IFoo>(b, priority: 200);

        var got = reg.Get<IFoo>();
        got.Should().BeSameAs(b);
    }

    [Fact]
    public void GetAll_Returns_In_Priority_Order()
    {
        var reg = new ServiceRegistry();
        var a = new FooA();
        var b = new FooB();

        reg.Register<IFoo>(a, priority: 50);
        reg.Register<IFoo>(b, priority: 500);

        var all = reg.GetAll<IFoo>().ToArray();
        all.Should().HaveCount(2);
        all[0].Should().BeSameAs(b);
        all[1].Should().BeSameAs(a);
    }

    [Fact]
    public void Get_SelectionMode_One_Throws_On_Multiple()
    {
        var reg = new ServiceRegistry();
        reg.Register<IFoo>(new FooA(), 10);
        reg.Register<IFoo>(new FooB(), 20);

        Action act = () => reg.Get<IFoo>(SelectionMode.One);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Get_SelectionMode_One_Works_On_Single()
    {
        var reg = new ServiceRegistry();
        var a = new FooA();
        reg.Register<IFoo>(a, 10);

        var got = reg.Get<IFoo>(SelectionMode.One);
        got.Should().BeSameAs(a);
    }

    [Fact]
    public void Unregister_Works()
    {
        var reg = new ServiceRegistry();
        var a = new FooA();
        reg.Register<IFoo>(a, 10);
        reg.IsRegistered<IFoo>().Should().BeTrue();

        var removed = reg.Unregister<IFoo>(a);
        removed.Should().BeTrue();
        reg.IsRegistered<IFoo>().Should().BeFalse();
    }
}
