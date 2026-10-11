using ChaosLab.Analysis;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class BasinEntropyTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void SingleAttractor_ReturnsZero()
    {
        var basins = new BasinResult
        {
            Width = 10,
            Height = 10,
            Labels = Enumerable.Repeat(0, 100).ToArray(),
            AttractorCount = 1,
        };

        BasinEntropy.ShannonEntropy(basins).Should().Be(0.0);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void EvenSplitOfFour_ReturnsLog2OfFour()
    {
        var labels = new int[100];
        for (int i = 0; i < 100; i++) labels[i] = i % 4;

        var basins = new BasinResult
        {
            Width = 10,
            Height = 10,
            Labels = labels,
            AttractorCount = 4,
        };

        double H = BasinEntropy.ShannonEntropy(basins);
        H.Should().BeApproximately(2.0, 1e-9);

        double Hn = BasinEntropy.NormalizedEntropy(basins);
        Hn.Should().BeApproximately(1.0, 1e-9);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void EmptyBasins_ReturnsNaN()
    {
        var basins = new BasinResult
        {
            Width = 0,
            Height = 0,
            Labels = Array.Empty<int>(),
            AttractorCount = 0,
        };

        double.IsNaN(BasinEntropy.ShannonEntropy(basins)).Should().BeTrue();
    }
}
