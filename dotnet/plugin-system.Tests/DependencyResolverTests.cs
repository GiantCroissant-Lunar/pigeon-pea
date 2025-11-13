using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.PluginSystem;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class DependencyResolverTests
{
    [Fact]
    public void ResolveLoadOrder_Respects_Dependencies()
    {
        var a = new PluginManifest { Id = "A", Name = "A", Version = "1.0.0" };
        var b = new PluginManifest { Id = "B", Name = "B", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "A" } } };
        var c = new PluginManifest { Id = "C", Name = "C", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "B" } } };

        var order = DependencyResolver.ResolveLoadOrder(new[] { c, b, a }).Select(m => m.Id).ToArray();
        order.Should().ContainInOrder("A", "B", "C");
    }

    [Fact]
    public void ResolveLoadOrder_Allows_Optional_Missing()
    {
        var a = new PluginManifest { Id = "A", Name = "A", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "X", Optional = true } } };
        var order = DependencyResolver.ResolveLoadOrder(new[] { a }).Select(m => m.Id).ToArray();
        order.Should().ContainSingle().Which.Should().Be("A");
    }

    [Fact]
    public void ResolveLoadOrder_Throws_On_Missing_Required_Dependency()
    {
        var a = new PluginManifest { Id = "A", Name = "A", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "Missing", Optional = false } } };
        var act = () => DependencyResolver.ResolveLoadOrder(new[] { a });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*depends on missing plugin*");
    }

    [Fact]
    public void ResolveLoadOrder_Throws_On_Cycle()
    {
        var a = new PluginManifest { Id = "A", Name = "A", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "B" } } };
        var b = new PluginManifest { Id = "B", Name = "B", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "A" } } };
        var act = () => DependencyResolver.ResolveLoadOrder(new[] { a, b });
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cyclic plugin dependency detected*");
    }

    [Fact]
    public void ResolveLoadOrder_Orders_Shared_Dependency_Before_Dependents()
    {
        // B is a shared dependency of A and C
        var b = new PluginManifest { Id = "B", Name = "B", Version = "1.0.0" };
        var a = new PluginManifest { Id = "A", Name = "A", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "B" } } };
        var c = new PluginManifest { Id = "C", Name = "C", Version = "1.0.0", Dependencies = new List<PluginDependency> { new() { Id = "B" } } };

        var order = DependencyResolver.ResolveLoadOrder(new[] { a, b, c }).Select(m => m.Id).ToArray();
        var idxB = System.Array.IndexOf(order, "B");
        var idxA = System.Array.IndexOf(order, "A");
        var idxC = System.Array.IndexOf(order, "C");

        idxB.Should().BeGreaterThanOrEqualTo(0);
        idxA.Should().BeGreaterThan(idxB);
        idxC.Should().BeGreaterThan(idxB);
    }
}
