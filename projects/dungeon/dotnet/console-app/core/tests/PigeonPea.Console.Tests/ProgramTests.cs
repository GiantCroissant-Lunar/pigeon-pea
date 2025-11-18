using System.CommandLine;
using Xunit;

namespace PigeonPea.Console.Tests;

/// <summary>
/// Unit tests for CLI argument parsing in Program class.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void ParseArgumentsWithNoArgumentsUsesDefaults()
    {
        // Arrange
        var (rootCommand, rendererOption, debugOption, widthOption, heightOption) = CreateRootCommand();
        var args = Array.Empty<string>();

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("auto", parseResult.GetValue(rendererOption));
        Assert.False(parseResult.GetValue(debugOption));
        Assert.Null(parseResult.GetValue(widthOption));
        Assert.Null(parseResult.GetValue(heightOption));
    }

    [Fact]
    public void ParseArgumentsWithRendererOptionParsesCorrectly()
    {
        // Arrange
        var (rootCommand, rendererOption, _, _, _) = CreateRootCommand();
        var args = new[] { "--renderer", "kitty" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("kitty", parseResult.GetValue(rendererOption));
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("kitty")]
    [InlineData("sixel")]
    [InlineData("braille")]
    [InlineData("ascii")]
    public void ParseArgumentsWithValidRendererValuesParsesCorrectly(string renderer)
    {
        // Arrange
        var (rootCommand, rendererOption, _, _, _) = CreateRootCommand();
        var args = new[] { "--renderer", renderer };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(renderer, parseResult.GetValue(rendererOption));
    }

    [Fact]
    public void ParseArgumentsWithDebugOptionParsesCorrectly()
    {
        // Arrange
        var (rootCommand, _, debugOption, _, _) = CreateRootCommand();
        var args = new[] { "--debug" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.True(parseResult.GetValue(debugOption));
    }

    [Fact]
    public void ParseArgumentsWithWidthAndHeightParsesCorrectly()
    {
        // Arrange
        var (rootCommand, _, _, widthOption, heightOption) = CreateRootCommand();
        var args = new[] { "--width", "120", "--height", "40" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(120, parseResult.GetValue(widthOption));
        Assert.Equal(40, parseResult.GetValue(heightOption));
    }

    [Fact]
    public void ParseArgumentsWithAllOptionsParsesCorrectly()
    {
        // Arrange
        var (rootCommand, rendererOption, debugOption, widthOption, heightOption) = CreateRootCommand();
        var args = new[] { "--renderer", "sixel", "--debug", "--width", "100", "--height", "30" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("sixel", parseResult.GetValue(rendererOption));
        Assert.True(parseResult.GetValue(debugOption));
        Assert.Equal(100, parseResult.GetValue(widthOption));
        Assert.Equal(30, parseResult.GetValue(heightOption));
    }

    [Fact]
    public void ParseArgumentsWithInvalidWidthReportsError()
    {
        // Arrange
        var (rootCommand, _, _, _, _) = CreateRootCommand();
        var args = new[] { "--width", "invalid" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void ParseArgumentsWithInvalidHeightReportsError()
    {
        // Arrange
        var (rootCommand, _, _, _, _) = CreateRootCommand();
        var args = new[] { "--height", "invalid" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void ParseArgumentsHelpOptionIsSupported()
    {
        // Arrange
        var (rootCommand, _, _, _, _) = CreateRootCommand();
        var args = new[] { "--help" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert - help should be recognized (no errors about unrecognized option)
        Assert.All(parseResult.Errors, error =>
            Assert.DoesNotContain("--help", error.Message));
    }

    // Helper methods to create command structure matching Program.cs
    private static (RootCommand, Option<string>, Option<bool>, Option<int?>, Option<int?>) CreateRootCommand()
    {
        var rendererOption = new Option<string>("--renderer")
        {
            Description = "Renderer to use (auto, kitty, sixel, braille, ascii)",
            DefaultValueFactory = _ => "auto"
        };

        var debugOption = new Option<bool>("--debug")
        {
            Description = "Enable debug mode"
        };

        var widthOption = new Option<int?>("--width")
        {
            Description = "Window width in characters"
        };

        var heightOption = new Option<int?>("--height")
        {
            Description = "Window height in characters"
        };

        var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");
        rootCommand.Add(rendererOption);
        rootCommand.Add(debugOption);
        rootCommand.Add(widthOption);
        rootCommand.Add(heightOption);

        return (rootCommand, rendererOption, debugOption, widthOption, heightOption);
    }
}
