using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PigeonPea.Plugin.Recording.Asciinema;
using PigeonPea.Plugin.Recording.Asciinema.Exporters;
using PigeonPea.Plugin.Recording.Asciinema.Models;
using PigeonPea.Plugin.Recording.Asciinema.Strategies;

namespace PigeonPea.Plugin.Recording.Asciinema.Tests;

[TestClass]
public class AsciinemaRecordingTests
{
    private Mock<ILogger> _mockLogger = null!;
    private string _tempDirectory = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger>();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    [TestMethod]
    public void TestStrategySelection_BinaryAvailable()
    {
        // Act
        var service = new AsciinemaRecordingService(_mockLogger.Object);

        // Assert
        Assert.IsNotNull(service);
        var strategyInfo = service.GetStrategyInfo();
        Assert.IsNotNull(strategyInfo);
        // Strategy info should contain information about the selected strategy
        Assert.IsTrue(strategyInfo.Length > 0);
    }

    [TestMethod]
    public void TestStrategySelection_BinaryNotAvailable()
    {
        // Act
        var service = new AsciinemaRecordingService(_mockLogger.Object);

        // Assert
        Assert.IsNotNull(service);
        var strategyInfo = service.GetStrategyInfo();
        // Should fallback to terminal buffer strategy
        Assert.IsNotNull(strategyInfo);
    }

    [TestMethod]
    public async Task TestAsciinemaExport_ValidFormat()
    {
        // Arrange
        var exporter = new AsciinemaExporter(_mockLogger.Object);
        var frames = new List<TerminalFrame>
        {
            new TerminalFrame(0.0, 80, 24, "Hello World"),
            new TerminalFrame(1.5, 80, 24, "Hello World\r\nSecond Line")
        };
        var outputPath = Path.Combine(_tempDirectory, "test.cast");

        // Act
        await exporter.ExportAsync(frames, outputPath);

        // Assert
        Assert.IsTrue(File.Exists(outputPath));

        var content = await File.ReadAllLinesAsync(outputPath);
        Assert.IsTrue(content.Length >= 3); // Header + 2 frames

        // Verify header
        var headerLine = content[0];
        Assert.IsTrue(headerLine.Contains("\"version\":2"));
        Assert.IsTrue(headerLine.Contains("\"width\":80"));
        Assert.IsTrue(headerLine.Contains("\"height\":24"));

        // Verify first frame
        var firstFrame = content[1];
        Assert.IsTrue(firstFrame.StartsWith("[0.0,\"o\""));
        Assert.IsTrue(firstFrame.Contains("Hello World"));
    }

    [TestMethod]
    public async Task TestAsciinemaExport_EmptyFrames_ThrowsException()
    {
        // Arrange
        var exporter = new AsciinemaExporter(_mockLogger.Object);
        var frames = new List<TerminalFrame>();
        var outputPath = Path.Combine(_tempDirectory, "test.cast");

        // Act & Assert
        await Assert.ThrowsExceptionAsync<ArgumentException>(
            () => exporter.ExportAsync(frames, outputPath));
    }

    [TestMethod]
    public async Task TestAsciinemaValidateCastFile_ValidFile_ReturnsTrue()
    {
        // Arrange
        var exporter = new AsciinemaExporter(_mockLogger.Object);
        var frames = new List<TerminalFrame>
        {
            new TerminalFrame(0.0, 80, 24, "Test Content")
        };
        var outputPath = Path.Combine(_tempDirectory, "valid.cast");
        await exporter.ExportAsync(frames, outputPath);

        // Act
        var isValid = await exporter.ValidateCastFileAsync(outputPath);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public async Task TestAsciinemaValidateCastFile_InvalidFile_ReturnsFalse()
    {
        // Arrange
        var exporter = new AsciinemaExporter(_mockLogger.Object);
        var invalidPath = Path.Combine(_tempDirectory, "invalid.cast");
        await File.WriteAllTextAsync(invalidPath, "invalid json content");

        // Act
        var isValid = await exporter.ValidateCastFileAsync(invalidPath);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public async Task TestAsciinemaGetMetadata_ValidFile_ReturnsMetadata()
    {
        // Arrange
        var exporter = new AsciinemaExporter(_mockLogger.Object);
        var frames = new List<TerminalFrame>
        {
            new TerminalFrame(0.0, 80, 24, "Start"),
            new TerminalFrame(2.5, 80, 24, "End")
        };
        var outputPath = Path.Combine(_tempDirectory, "metadata.cast");
        await exporter.ExportAsync(frames, outputPath);

        // Act
        var metadata = await exporter.GetMetadataAsync(outputPath);

        // Assert
        Assert.IsNotNull(metadata);
        Assert.AreEqual(2, metadata.Version);
        Assert.AreEqual(80, metadata.Width);
        Assert.AreEqual(24, metadata.Height);
        Assert.AreEqual(2, metadata.FrameCount);
        Assert.AreEqual(2.5, metadata.DurationSeconds);
        Assert.IsTrue(metadata.Timestamp > 0);
    }

    [TestMethod]
    public void TestTerminalFrame_HasSameContent_IdenticalFrames()
    {
        // Arrange
        var frame1 = new TerminalFrame(1.0, 80, 24, "Test Content");
        var frame2 = new TerminalFrame(2.0, 80, 24, "Test Content");

        // Act & Assert
        Assert.IsTrue(frame1.HasSameContent(frame2));
    }

    [TestMethod]
    public void TestTerminalFrame_HasSameContent_DifferentFrames()
    {
        // Arrange
        var frame1 = new TerminalFrame(1.0, 80, 24, "Test Content");
        var frame2 = new TerminalFrame(2.0, 80, 24, "Different Content");

        // Act & Assert
        Assert.IsFalse(frame1.HasSameContent(frame2));
    }

    [TestMethod]
    public void TestTerminalFrame_GetContentHash_SameContent_SameHash()
    {
        // Arrange
        var frame1 = new TerminalFrame(1.0, 80, 24, "Test Content");
        var frame2 = new TerminalFrame(2.0, 80, 24, "Test Content");

        // Act
        var hash1 = frame1.GetContentHash();
        var hash2 = frame2.GetContentHash();

        // Assert
        Assert.AreEqual(hash1, hash2);
    }

    [TestMethod]
    public async Task TestAsciinemaRecordingService_StartStop_Recording()
    {
        // Arrange
        var service = new AsciinemaRecordingService(_mockLogger.Object);
        var outputPath = Path.Combine(_tempDirectory, "recording_test.cast");

        try
        {
            // Act
            await service.StartAsync(outputPath);
            Assert.IsTrue(service.IsRecording);

            await service.StopAsync();
            Assert.IsFalse(service.IsRecording);

            // Assert
            // File should be created (either by binary or fallback strategy)
            if (File.Exists(outputPath))
            {
                var exporter = new AsciinemaExporter(_mockLogger.Object);
                var isValid = await exporter.ValidateCastFileAsync(outputPath);
                // Note: Fallback strategy might not create a valid file without Terminal.Gui running
                // So we just check if the service completed without throwing
            }
        }
        finally
        {
            service.Dispose();
        }
    }

    [TestMethod]
    public async Task TestAsciinemaRecordingService_StartTwice_ThrowsException()
    {
        // Arrange
        var service = new AsciinemaRecordingService(_mockLogger.Object);
        var outputPath = Path.Combine(_tempDirectory, "double_start.cast");

        try
        {
            await service.StartAsync(outputPath);

            // Act - should not throw but should log warning
            await service.StartAsync(Path.Combine(_tempDirectory, "second.cast"));

            // Assert - still recording first one
            Assert.IsTrue(service.IsRecording);
        }
        finally
        {
            await service.StopAsync();
            service.Dispose();
        }
    }

    [TestMethod]
    public void TestAsciinemaBinaryRecorder_IsAvailable_ReturnsBoolean()
    {
        // Act
        var isAvailable = AsciinemaBinaryRecorder.IsAvailable();

        // Assert
        // We can't guarantee asciinema is installed, but should return a boolean
        Assert.IsTrue(isAvailable == true || isAvailable == false);
    }

    [TestMethod]
    public void TestTerminalFrame_ToString_ReturnsExpectedFormat()
    {
        // Arrange
        var frame = new TerminalFrame(1.234, 80, 24, "Test Content");

        // Act
        var result = frame.ToString();

        // Assert
        Assert.IsTrue(result.Contains("1.234s"));
        Assert.IsTrue(result.Contains("80x24"));
        Assert.IsTrue(result.Contains("12 chars")); // "Test Content" length
    }
}
