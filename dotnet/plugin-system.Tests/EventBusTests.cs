using System.Threading.Tasks;
using FluentAssertions;
using PigeonPea.PluginSystem;
using Xunit;
using System;
using System.Collections.Generic;

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

    [Fact]
    public async Task Publish_NullEvent_Throws()
    {
        var bus = new EventBus();
        Func<Task> act = () => bus.PublishAsync<string>(null!);
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Publish_AsyncHandler_IsAwaited()
    {
        var bus = new EventBus();
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var observed = false;

        bus.Subscribe<string>(async s =>
        {
            await Task.Delay(30);
            observed = true;
            tcs.SetResult(true);
        });

        await bus.PublishAsync("go");
        await tcs.Task; // ensure handler ran
        observed.Should().BeTrue();
    }

    [Fact]
    public async Task Publish_HandlerThrows_CollectsExceptionsAndRunsAll()
    {
        var bus = new EventBus();
        var ran1 = false;
        var ran2 = false;
        var ran3 = false;

        bus.Subscribe<string>(s => { ran1 = true; return Task.CompletedTask; });
        bus.Subscribe<string>(s => { throw new InvalidOperationException("boom"); });
        bus.Subscribe<string>(s => { ran2 = true; throw new ArgumentException("crash"); });
        bus.Subscribe<string>(s => { ran3 = true; return Task.CompletedTask; });

        Func<Task> act = () => bus.PublishAsync("x");

        // Should throw AggregateException containing all handler exceptions
        var exception = await act.Should().ThrowAsync<AggregateException>();
        exception.Which.InnerExceptions.Should().HaveCount(2);
        exception.Which.InnerExceptions.Should().Contain(ex => ex is InvalidOperationException);
        exception.Which.InnerExceptions.Should().Contain(ex => ex is ArgumentException);

        // All handlers should have run despite exceptions in some
        ran1.Should().BeTrue();
        ran2.Should().BeTrue();
        ran3.Should().BeTrue();
    }

    [Fact]
    public async Task Publish_Concurrent_IsThreadSafe()
    {
        var bus = new EventBus();
        int count = 0;
        bus.Subscribe<string>(s => { System.Threading.Interlocked.Increment(ref count); return Task.CompletedTask; });
        bus.Subscribe<string>(s => { System.Threading.Interlocked.Increment(ref count); return Task.CompletedTask; });

        var tasks = new List<Task>();
        const int iterations = 25;
        for (int i = 0; i < iterations; i++)
        {
            tasks.Add(bus.PublishAsync("tick"));
        }

        await Task.WhenAll(tasks);
        count.Should().Be(iterations * 2);
    }
}
