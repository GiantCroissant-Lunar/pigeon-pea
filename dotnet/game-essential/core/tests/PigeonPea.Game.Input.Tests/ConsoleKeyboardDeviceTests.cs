using NexusInput.Bindings;
using NexusInput.Controls;
using PigeonPea.Game.Input.Devices;
using Xunit;

namespace PigeonPea.Game.Input.Tests;

public class ConsoleKeyboardDeviceTests
{
    [Fact]
    public void ConsoleKeyboardDeviceShouldHaveCorrectProperties()
    {
        // Arrange & Act
        var device = new ConsoleKeyboardDevice();

        // Assert
        device.DeviceId.Should().Be("Console-Keyboard");
        device.DeviceType.Should().Be("Keyboard");
    }

    [Fact]
    public void ConsoleKeyboardDeviceShouldMapKeysCorrectly()
    {
        // Arrange
        var device = new ConsoleKeyboardDevice();

        // Act & Assert - Test key mappings
        device.IsControlActive(new InputControlPath("<Keyboard>/w")).Should().BeFalse();
        device.IsControlActive(new InputControlPath("<Keyboard>/space")).Should().BeFalse();
        device.IsControlActive(new InputControlPath("<Keyboard>/uparrow")).Should().BeFalse();
    }

    [Fact]
    public void ConsoleKeyboardDeviceShouldReadControlValueCorrectly()
    {
        // Arrange
        var device = new ConsoleKeyboardDevice();
        var path = new InputControlPath("<Keyboard>/w");

        // Act
        var value = device.ReadControlValue(path);

        // Assert
        value.AsButton().Should().BeFalse();
    }

    [Fact]
    public void ConsoleKeyboardDeviceShouldIgnoreNonKeyboardPaths()
    {
        // Arrange
        var device = new ConsoleKeyboardDevice();
        var mousePath = new InputControlPath("<Mouse>/leftButton");

        // Act
        var isActive = device.IsControlActive(mousePath);
        var value = device.ReadControlValue(mousePath);

        // Assert
        isActive.Should().BeFalse();
        value.AsButton().Should().BeFalse();
    }
}
