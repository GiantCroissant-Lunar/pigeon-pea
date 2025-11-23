using FluentAssertions;
using PigeonPea.Camera2D.Core;
using PigeonPea.Camera2D.Math;
using Xunit;

namespace PigeonPea.Camera2D.Core.Tests;

public class Camera2DTests
{
    [Fact]
    public void CalculateTargetPosition_NoTargets_ReturnsCurrentPosition()
    {
        var camera = new Camera2D();
        camera.Transform.Position = new Vector2(10f, 5f);

        var result = camera.CalculateTargetPosition();

        result.Should().Be(camera.Transform.Position);
    }

    [Fact]
    public void CalculateTargetPosition_SingleTarget_ReturnsTargetPosition()
    {
        var camera = new Camera2D();
        var targetPosition = new Vector2(2f, 3f);

        camera.AddTarget(targetPosition);

        var result = camera.CalculateTargetPosition();

        result.Should().Be(targetPosition);
    }

    [Fact]
    public void CalculateTargetPosition_WeightedAverage_IsCorrect()
    {
        var camera = new Camera2D();

        camera.AddTarget(new Vector2(0f, 0f), weight: 0.75f);
        camera.AddTarget(new Vector2(10f, 0f), weight: 0.25f);

        var result = camera.CalculateTargetPosition();

        result.X.Should().BeApproximately(2.5f, 0.001f);
        result.Y.Should().BeApproximately(0f, 0.001f);
    }
}
