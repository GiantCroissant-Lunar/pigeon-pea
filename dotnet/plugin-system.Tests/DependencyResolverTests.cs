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
}
