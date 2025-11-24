using FluentAssertions;
using PigeonPea.Shared.Camera2D.Core;
using PigeonPea.Shared.Camera2D.Damping;
using PigeonPea.Shared.Camera2D.Math;
using Xunit;

namespace PigeonPea.Camera2D.Core.Tests;

public class FollowExtensionTests
{
    [Fact]
    public void FollowExtension_ExponentialDamping_MovesCameraTowardsTarget()
    {
        var camera = new Camera2D();
        camera.Transform.Position = new Vector2(0f, 0f);
        camera.AddTarget(new Vector2(10f, 0f));

        var follow = new FollowExtension
        {
            DampingType = DampingType.Exponential,
            Smoothness = 5f
        };

        camera.AddExtension(follow);

        camera.Update(0.1f);

        var newPos = camera.Transform.Position;
        newPos.X.Should().BeGreaterThan(0f).And.BeLessThan(10f);
        newPos.Y.Should().Be(0f);
    }
}
