using PigeonPea.Shared.Rendering;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.Rendering;

/// <summary>
/// Unit tests for the <see cref="ColorGradient"/> utility class.
/// </summary>
public class ColorGradientTests
{
    #region Lerp Tests

    [Fact]
    public void Lerp_WithT0_ReturnsFirstColor()
    {
        // Arrange
        var colorA = new Color(255, 128, 64, 255);
        var colorB = new Color(0, 0, 0, 255);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, 0f);

        // Assert
        Assert.Equal(colorA, result);
    }

    [Fact]
    public void Lerp_WithT1_ReturnsSecondColor()
    {
        // Arrange
        var colorA = new Color(255, 128, 64, 255);
        var colorB = new Color(0, 0, 0, 255);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, 1f);

        // Assert
        Assert.Equal(colorB, result);
    }

    [Fact]
    public void Lerp_WithT05_ReturnsMiddleColor()
    {
        // Arrange
        var colorA = new Color(100, 200, 50, 255);
        var colorB = new Color(200, 100, 150, 255);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, 0.5f);

        // Assert
        Assert.Equal((byte)150, result.R); // (100 + 200) / 2
        Assert.Equal((byte)150, result.G); // (200 + 100) / 2
        Assert.Equal((byte)100, result.B); // (50 + 150) / 2
        Assert.Equal((byte)255, result.A);
    }

    [Fact]
    public void Lerp_WithDifferentAlpha_InterpolatesAlpha()
    {
        // Arrange
        var colorA = new Color(255, 255, 255, 255);
        var colorB = new Color(255, 255, 255, 0);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, 0.5f);

        // Assert
        Assert.Equal((byte)127, result.A); // (255 + 0) / 2 ≈ 127
    }

    [Fact]
    public void Lerp_BlackToWhite_InterpolatesCorrectly()
    {
        // Arrange
        var black = Color.Black;
        var white = Color.White;

        // Act
        var result = ColorGradient.Lerp(black, white, 0.25f);

        // Assert
        Assert.Equal((byte)63, result.R); // 255 * 0.25 ≈ 63
        Assert.Equal((byte)63, result.G);
        Assert.Equal((byte)63, result.B);
    }

    [Fact]
    public void Lerp_RedToBlue_InterpolatesCorrectly()
    {
        // Arrange
        var red = Color.Red;
        var blue = Color.Blue;

        // Act
        var result = ColorGradient.Lerp(red, blue, 0.5f);

        // Assert
        Assert.Equal((byte)127, result.R); // Red decreases from 255 to 0
        Assert.Equal((byte)0, result.G);   // Green stays 0
        Assert.Equal((byte)127, result.B); // Blue increases from 0 to 255
    }

    #endregion

    #region CreateGradient Tests

    [Fact]
    public void CreateGradient_WithTwoSteps_ReturnsStartAndEnd()
    {
        // Arrange
        var start = Color.Red;
        var end = Color.Blue;

        // Act
        var gradient = ColorGradient.CreateGradient(start, end, 2);

        // Assert
        Assert.Equal(2, gradient.Length);
        Assert.Equal(start, gradient[0]);
        Assert.Equal(end, gradient[1]);
    }

    [Fact]
    public void CreateGradient_WithThreeSteps_ReturnsStartMiddleEnd()
    {
        // Arrange
        var start = new Color(0, 0, 0, 255);
        var end = new Color(100, 100, 100, 255);

        // Act
        var gradient = ColorGradient.CreateGradient(start, end, 3);

        // Assert
        Assert.Equal(3, gradient.Length);
        Assert.Equal((byte)0, gradient[0].R);
        Assert.Equal((byte)50, gradient[1].R);
        Assert.Equal((byte)100, gradient[2].R);
    }

    [Fact]
    public void CreateGradient_WithFiveSteps_ReturnsCorrectGradient()
    {
        // Arrange
        var start = Color.Black;
        var end = Color.White;

        // Act
        var gradient = ColorGradient.CreateGradient(start, end, 5);

        // Assert
        Assert.Equal(5, gradient.Length);
        Assert.Equal((byte)0, gradient[0].R);
        Assert.Equal((byte)63, gradient[1].R);  // 255 * 1/4 ≈ 63
        Assert.Equal((byte)127, gradient[2].R); // 255 * 2/4 ≈ 127
        Assert.Equal((byte)191, gradient[3].R); // 255 * 3/4 ≈ 191
        Assert.Equal((byte)255, gradient[4].R);
    }

    [Fact]
    public void CreateGradient_WithLessThanTwoSteps_ThrowsArgumentException()
    {
        // Arrange
        var start = Color.Red;
        var end = Color.Blue;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => ColorGradient.CreateGradient(start, end, 1));
        Assert.Throws<ArgumentException>(() => ColorGradient.CreateGradient(start, end, 0));
        Assert.Throws<ArgumentException>(() => ColorGradient.CreateGradient(start, end, -1));
    }

    [Fact]
    public void CreateGradient_WithSameStartAndEnd_ReturnsUniformGradient()
    {
        // Arrange
        var color = new Color(123, 45, 67, 89);

        // Act
        var gradient = ColorGradient.CreateGradient(color, color, 5);

        // Assert
        Assert.Equal(5, gradient.Length);
        Assert.All(gradient, c => Assert.Equal(color, c));
    }

    #endregion

    #region ApplyDistanceFade Tests

    [Fact]
    public void ApplyDistanceFade_WithZeroDistance_ReturnsBaseColor()
    {
        // Arrange
        var baseColor = Color.Red;
        float distance = 0f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(baseColor, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithMaxDistance_ReturnsBlack()
    {
        // Arrange
        var baseColor = Color.Yellow;
        float distance = 10f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithHalfDistance_ReturnsMiddleColor()
    {
        // Arrange
        var baseColor = new Color(200, 100, 50, 255);
        float distance = 5f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal((byte)100, result.R); // 200 * 0.5
        Assert.Equal((byte)50, result.G);  // 100 * 0.5
        Assert.Equal((byte)25, result.B);  // 50 * 0.5
    }

    [Fact]
    public void ApplyDistanceFade_WithDistanceBeyondMax_ReturnsClampedBlack()
    {
        // Arrange
        var baseColor = Color.White;
        float distance = 20f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithNegativeDistance_ReturnsClampedBaseColor()
    {
        // Arrange
        var baseColor = Color.Green;
        float distance = -5f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(baseColor, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithQuarterDistance_ReturnsFadedColor()
    {
        // Arrange
        var baseColor = new Color(100, 80, 60, 255);
        float distance = 2.5f;
        float maxDistance = 10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        // At t=0.25, color should be 75% of original
        Assert.Equal((byte)75, result.R); // 100 * 0.75
        Assert.Equal((byte)60, result.G); // 80 * 0.75
        Assert.Equal((byte)45, result.B); // 60 * 0.75
    }

    [Fact]
    public void ApplyDistanceFade_MultipleDistances_ProducesGradualFade()
    {
        // Arrange
        var baseColor = Color.Red;
        float maxDistance = 10f;

        // Act
        var fade0 = ColorGradient.ApplyDistanceFade(baseColor, 0f, maxDistance);
        var fade25 = ColorGradient.ApplyDistanceFade(baseColor, 2.5f, maxDistance);
        var fade50 = ColorGradient.ApplyDistanceFade(baseColor, 5f, maxDistance);
        var fade75 = ColorGradient.ApplyDistanceFade(baseColor, 7.5f, maxDistance);
        var fade100 = ColorGradient.ApplyDistanceFade(baseColor, 10f, maxDistance);

        // Assert - red channel should gradually decrease
        Assert.Equal((byte)255, fade0.R);
        Assert.Equal((byte)191, fade25.R); // 255 * 0.75
        Assert.Equal((byte)127, fade50.R); // 255 * 0.5
        Assert.Equal((byte)63, fade75.R);  // 255 * 0.25
        Assert.Equal((byte)0, fade100.R);
    }

    [Fact]
    public void ApplyDistanceFade_WithZeroMaxDistance_AndZeroDistance_ReturnsBaseColor()
    {
        // Arrange
        var baseColor = Color.Yellow;
        float distance = 0f;
        float maxDistance = 0f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(baseColor, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithZeroMaxDistance_AndPositiveDistance_ReturnsBlack()
    {
        // Arrange
        var baseColor = Color.Yellow;
        float distance = 5f;
        float maxDistance = 0f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithNegativeMaxDistance_AndZeroDistance_ReturnsBaseColor()
    {
        // Arrange
        var baseColor = Color.Blue;
        float distance = 0f;
        float maxDistance = -10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(baseColor, result);
    }

    [Fact]
    public void ApplyDistanceFade_WithNegativeMaxDistance_AndPositiveDistance_ReturnsBlack()
    {
        // Arrange
        var baseColor = Color.Blue;
        float distance = 5f;
        float maxDistance = -10f;

        // Act
        var result = ColorGradient.ApplyDistanceFade(baseColor, distance, maxDistance);

        // Assert
        Assert.Equal(Color.Black, result);
    }

    [Fact]
    public void Lerp_WithTBelowZero_ClampsToZero()
    {
        // Arrange
        var colorA = new Color(100, 150, 200, 255);
        var colorB = new Color(200, 100, 50, 255);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, -0.5f);

        // Assert
        Assert.Equal(colorA, result);
    }

    [Fact]
    public void Lerp_WithTAboveOne_ClampsToOne()
    {
        // Arrange
        var colorA = new Color(100, 150, 200, 255);
        var colorB = new Color(200, 100, 50, 255);

        // Act
        var result = ColorGradient.Lerp(colorA, colorB, 1.5f);

        // Assert
        Assert.Equal(colorB, result);
    }

    #endregion
}
