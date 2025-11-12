using System.Threading.Tasks;
using FluentAssertions;
using PigeonPea.PluginSystem;
using Xunit;

namespace PigeonPea.PluginSystem.Tests;

public class EventBusTests
{
    [Fact]
    public async Task Publish_Invokes_All_Subscribers()
    {
        var bus = new EventBus();
        int c1 = 0, c2 = 0;

        bus.Subscribe<string>(s => { c1++; return Task.CompletedTask; });
        bus.Subscribe<string>(s => { c2++; return Task.CompletedTask; });

        await bus.PublishAsync("hello");

        c1.Should().Be(1);
        c2.Should().Be(1);
    }
}
