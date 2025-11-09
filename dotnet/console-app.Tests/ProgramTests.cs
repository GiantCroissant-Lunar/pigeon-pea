using System.CommandLine;
using System.CommandLine.Parsing;
using Xunit;

namespace PigeonPea.Console.Tests;

/// <summary>
/// Unit tests for CLI argument parsing in Program class.
/// </summary>
public class ProgramTests
{
    [Fact]
    public void ParseArguments_WithNoArguments_UsesDefaults()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = Array.Empty<string>();

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("auto", parseResult.GetValueForOption(GetRendererOption(rootCommand)));
        Assert.False(parseResult.GetValueForOption(GetDebugOption(rootCommand)));
        Assert.Null(parseResult.GetValueForOption(GetWidthOption(rootCommand)));
        Assert.Null(parseResult.GetValueForOption(GetHeightOption(rootCommand)));
    }

    [Fact]
    public void ParseArguments_WithRendererOption_ParsesCorrectly()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--renderer", "kitty" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("kitty", parseResult.GetValueForOption(GetRendererOption(rootCommand)));
    }

    [Theory]
    [InlineData("auto")]
    [InlineData("kitty")]
    [InlineData("sixel")]
    [InlineData("braille")]
    [InlineData("ascii")]
    public void ParseArguments_WithValidRendererValues_ParsesCorrectly(string renderer)
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--renderer", renderer };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(renderer, parseResult.GetValueForOption(GetRendererOption(rootCommand)));
    }

    [Fact]
    public void ParseArguments_WithDebugOption_ParsesCorrectly()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--debug" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.True(parseResult.GetValueForOption(GetDebugOption(rootCommand)));
    }

    [Fact]
    public void ParseArguments_WithWidthAndHeight_ParsesCorrectly()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--width", "120", "--height", "40" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal(120, parseResult.GetValueForOption(GetWidthOption(rootCommand)));
        Assert.Equal(40, parseResult.GetValueForOption(GetHeightOption(rootCommand)));
    }

    [Fact]
    public void ParseArguments_WithAllOptions_ParsesCorrectly()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--renderer", "sixel", "--debug", "--width", "100", "--height", "30" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.Empty(parseResult.Errors);
        Assert.Equal("sixel", parseResult.GetValueForOption(GetRendererOption(rootCommand)));
        Assert.True(parseResult.GetValueForOption(GetDebugOption(rootCommand)));
        Assert.Equal(100, parseResult.GetValueForOption(GetWidthOption(rootCommand)));
        Assert.Equal(30, parseResult.GetValueForOption(GetHeightOption(rootCommand)));
    }

    [Fact]
    public void ParseArguments_WithInvalidWidth_ReportsError()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--width", "invalid" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void ParseArguments_WithInvalidHeight_ReportsError()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--height", "invalid" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert
        Assert.NotEmpty(parseResult.Errors);
    }

    [Fact]
    public void ParseArguments_HelpOption_IsSupported()
    {
        // Arrange
        var rootCommand = CreateRootCommand();
        var args = new[] { "--help" };

        // Act
        var parseResult = rootCommand.Parse(args);

        // Assert - help should be recognized (no errors about unrecognized option)
        Assert.All(parseResult.Errors, error =>
            Assert.DoesNotContain("--help", error.Message));
    }

    // Helper methods to create command structure matching Program.cs
    private static RootCommand CreateRootCommand()
    {
        var rootCommand = new RootCommand("Pigeon Pea - Roguelike Dungeon Crawler");

        var rendererOption = new Option<string>(
            name: "--renderer",
            description: "Renderer to use (auto, kitty, sixel, braille, ascii)",
            getDefaultValue: () => "auto"
        );
        rootCommand.AddOption(rendererOption);

        var debugOption = new Option<bool>(
            name: "--debug",
            description: "Enable debug mode"
        );
        rootCommand.AddOption(debugOption);

        var widthOption = new Option<int?>(
            name: "--width",
            description: "Window width in characters"
        );
        rootCommand.AddOption(widthOption);

        var heightOption = new Option<int?>(
            name: "--height",
            description: "Window height in characters"
        );
        rootCommand.AddOption(heightOption);

        return rootCommand;
    }

    private static Option<string> GetRendererOption(RootCommand command)
    {
        return (Option<string>)command.Options.First(o => o.Name == "renderer");
    }

    private static Option<bool> GetDebugOption(RootCommand command)
    {
        return (Option<bool>)command.Options.First(o => o.Name == "debug");
    }

    private static Option<int?> GetWidthOption(RootCommand command)
    {
        return (Option<int?>)command.Options.First(o => o.Name == "width");
    }

    private static Option<int?> GetHeightOption(RootCommand command)
    {
        return (Option<int?>)command.Options.First(o => o.Name == "height");
    }
}
