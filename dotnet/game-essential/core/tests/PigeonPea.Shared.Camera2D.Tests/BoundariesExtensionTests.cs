using FluentAssertions;
using PigeonPea.Shared.Camera2D.Core;
using PigeonPea.Shared.Camera2D.Math;
using Xunit;

namespace PigeonPea.Camera2D.Core.Tests;

public class BoundariesExtensionTests
{
    [Fact]
    public void BoundariesExtension_ClampsPositionWithinBounds()
    {
        var camera = new Camera2D();
        camera.Transform.Position = new Vector2(0f, 0f);

        var bounds = new Rect { X = -5, Y = -5, Width = 10, Height = 10 };
        var extension = new BoundariesExtension { Boundaries = bounds };

        camera.AddExtension(extension);
        camera.Transform.Position = new Vector2(100f, 100f);

        camera.Update(0.1f);

        camera.Transform.Position.X.Should().BeLessOrEqualTo(bounds.X + bounds.Width);
        camera.Transform.Position.Y.Should().BeLessOrEqualTo(bounds.Y + bounds.Height);
    }
}
