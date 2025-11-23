using FluentAssertions;
using PigeonPea.Camera2D.Core;
using PigeonPea.Camera2D.Math;
using Xunit;

namespace PigeonPea.Camera2D.Core.Tests;

public class DeadzoneExtensionTests
{
    [Fact]
    public void DeadzoneExtension_InsideDeadzone_DoesNotMoveCamera()
    {
        var camera = new Camera2D();
        camera.Transform.Position = new Vector2(0f, 0f);
        camera.AddTarget(new Vector2(1f, 1f));

        var deadzone = new DeadzoneExtension
        {
            DeadzoneWidth = 10f,
            DeadzoneHeight = 10f,
            SoftEdge = 0f
        };

        camera.AddExtension(deadzone);

        camera.Update(1.0f);

        camera.Transform.Position.X.Should().BeApproximately(0f, 0.0001f);
        camera.Transform.Position.Y.Should().BeApproximately(0f, 0.0001f);
    }
}
