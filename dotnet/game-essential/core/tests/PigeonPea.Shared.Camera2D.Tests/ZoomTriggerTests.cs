using FluentAssertions;
using PigeonPea.Shared.Camera2D.Core;
using PigeonPea.Shared.Camera2D.Math;
using PigeonPea.Shared.Camera2D.Triggers;
using Xunit;

namespace PigeonPea.Camera2D.Core.Tests;

public class ZoomTriggerTests
{
    [Fact]
    public void ZoomTrigger_WithinRadius_AdjustsZoom()
    {
        var camera = new Camera2D();
        camera.Transform.Position = new Vector2(0f, 0f);
        camera.Zoom = 1.0f;

        var trigger = new ZoomTrigger
        {
            Center = new Vector2(0f, 0f),
            Radius = 5f,
            TargetZoom = 2.0f
        };

        camera.AddTrigger(trigger);
        camera.AddTarget(new Vector2(1f, 1f));

        camera.Update(0.1f);

        camera.Zoom.Should().BeGreaterThan(1.0f).And.BeLessOrEqualTo(2.0f);
    }
}
