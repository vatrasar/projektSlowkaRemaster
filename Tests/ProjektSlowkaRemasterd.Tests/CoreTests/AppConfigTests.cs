using System;
using System.IO;
using Xunit;
using ProjektSlowkaRemasterd.Src.Core.Config;

namespace ProjektSlowkaRemasterd.Tests.CoreTests;

public class AppConfigTests
{
    [Fact]
    public void ResolvedDatabasePath_WhenEmpty_ReturnsDefaultPath()
    {
        // Arrange
        var config = new AppConfig { DatabasePath = "" };
        var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "slowka.db");

        // Act
        var result = config.ResolvedDatabasePath;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvedDatabasePath_WhenRelative_ReturnsRelativeCombinedWithBaseDir()
    {
        // Arrange
        var relativePath = Path.Combine("custom_dir", "app.db");
        var config = new AppConfig { DatabasePath = relativePath };
        var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // Act
        var result = config.ResolvedDatabasePath;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvedDatabasePath_WhenAbsolute_ReturnsAbsolutePathDirectly()
    {
        // Arrange
        var absolutePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "absolute_test.db"));
        var config = new AppConfig { DatabasePath = absolutePath };

        // Act
        var result = config.ResolvedDatabasePath;

        // Assert
        Assert.Equal(absolutePath, result);
    }

    [Fact]
    public void ResolvedMediaDirectoryPath_WhenEmpty_ReturnsDefaultPath()
    {
        // Arrange
        var config = new AppConfig { MediaDirectoryPath = "" };
        var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media");

        // Act
        var result = config.ResolvedMediaDirectoryPath;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvedMediaDirectoryPath_WhenRelative_ReturnsRelativeCombinedWithBaseDir()
    {
        // Arrange
        var relativePath = "custom_media";
        var config = new AppConfig { MediaDirectoryPath = relativePath };
        var expected = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath);

        // Act
        var result = config.ResolvedMediaDirectoryPath;

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ResolvedMediaDirectoryPath_WhenAbsolute_ReturnsAbsolutePathDirectly()
    {
        // Arrange
        var absolutePath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "absolute_media_dir"));
        var config = new AppConfig { MediaDirectoryPath = absolutePath };

        // Act
        var result = config.ResolvedMediaDirectoryPath;

        // Assert
        Assert.Equal(absolutePath, result);
    }
}
