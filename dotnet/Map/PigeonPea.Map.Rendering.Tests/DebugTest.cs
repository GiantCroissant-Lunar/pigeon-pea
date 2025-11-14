using SkiaSharp;
using Xunit;
using PigeonPea.Map.Rendering;

namespace PigeonPea.Map.Rendering.Tests;

public class DebugTest
{
    [Fact]
    public void DebugLerp()
    {
        // Arrange
        SKColor black = new SKColor(0, 0, 0);
        SKColor white = new SKColor(255, 255, 255);
        double t = 0.5;

        // Act
        SKColor result = ColorSchemes.Lerp(black, white, t);

        // Debug
        Console.WriteLine($"t: {t}");
        Console.WriteLine($"Black: R={black.Red}, G={black.Green}, B={black.Blue}");
        Console.WriteLine($"White: R={white.Red}, G={white.Green}, B={white.Blue}");
        Console.WriteLine($"Result: R={result.Red}, G={result.Green}, B={result.Blue}");

        // Manual calculation
        double diffR = white.Red - black.Red;
        double diffG = white.Green - black.Green;
        double diffB = white.Blue - black.Blue;
        double calcR = black.Red + diffR * t;
        double calcG = black.Green + diffG * t;
        double calcB = black.Blue + diffB * t;

        Console.WriteLine($"Diff: R={diffR}, G={diffG}, B={diffB}");
        Console.WriteLine($"Calc: R={calcR}, G={calcG}, B={calcB}");
        Console.WriteLine($"Cast: R={(byte)calcR}, G={(byte)calcG}, B={(byte)calcB}");

        // Assert
        Assert.InRange(result.Red, (byte)126, (byte)128);
        Assert.InRange(result.Green, (byte)126, (byte)128);
        Assert.InRange(result.Blue, (byte)126, (byte)128);
    }
}
