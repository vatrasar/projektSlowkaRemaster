using Xunit;
using ProjektSlowkaRemasterd.Src.Features.Review.UI.Screens.ReviewSession;

namespace ProjektSlowkaRemasterd.Tests.CoreTests;

public class AlgorithmTests
{
    [Theory]
    [InlineData(0, 0, 1)] // New item (interval 0, failures 0) -> 1
    [InlineData(1, 0, 3)] // Normal progression: 1 -> 3
    [InlineData(3, 0, 10)] // Normal progression: 3 -> 10
    [InlineData(10, 0, 30)] // Normal progression: 10 -> 30
    [InlineData(1, 2, 3)] // Hard mode progression (failures >= 2): 1 -> 3
    [InlineData(3, 2, 6)] // Hard mode: 3 -> 6
    [InlineData(6, 2, 10)] // Hard mode: 6 -> 10
    [InlineData(10, 2, 20)] // Hard mode: 10 -> 20
    [InlineData(20, 2, 30)] // Hard mode: 20 -> 30
    [InlineData(30, 2, 30)] // Cap at 30
    public void GetNextInterval_ReturnsCorrectNextInterval(int currentInterval, int failures, int expectedNext)
    {
        // Act
        var result = ReviewSessionViewModel.GetNextInterval(currentInterval, failures);

        // Assert
        Assert.Equal(expectedNext, result);
    }
}
