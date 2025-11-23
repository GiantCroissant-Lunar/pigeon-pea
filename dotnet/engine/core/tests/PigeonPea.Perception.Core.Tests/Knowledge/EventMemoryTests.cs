using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using PigeonPea.Perception.Models;
using Xunit;

namespace PigeonPea.Perception.Core.Tests.Knowledge;

public class EventMemoryTests
{
    [Fact]
    public void RecordEvent_AddsEvent()
    {
        var memory = new EventMemory();
        var entry = new MemoryEntry
        {
            Timestamp = 10f,
            EventType = "EnemySpotted",
            Details = "Saw player"
        };

        memory.RecordEvent(entry);

        memory.Events.Should().Contain(entry);
    }

    [Fact]
    public void GetRecentEvents_FiltersCorrectly()
    {
        var memory = new EventMemory();
        memory.RecordEvent(new MemoryEntry { Timestamp = 5f, EventType = "Old" });
        memory.RecordEvent(new MemoryEntry { Timestamp = 18f, EventType = "Recent" });

        var recent = memory.GetRecentEvents(currentTime: 20f, seconds: 5f).ToList();

        recent.Should().HaveCount(1);
        recent[0].EventType.Should().Be("Recent");
    }

    [Fact]
    public void RecordEvent_CleansUpOldEvents_WhenOverMax()
    {
        var memory = new EventMemory { MaxEvents = 5 };

        for (int i = 0; i < 10; i++)
        {
            memory.RecordEvent(new MemoryEntry { Timestamp = i });
        }

        memory.Events.Should().HaveCount(5);
    }
}
