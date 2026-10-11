using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class DefaultLyapunovAxesTests
{
    [Theory]
    [Trait(TestCategories.Fast, "true")]
    [InlineData("Lorenz", 1, 0)]
    [InlineData("Rössler", 2, 0)]
    [InlineData("Chen", 2, 0)]
    [InlineData("Hénon", 0, 1)]
    [Trait(TestCategories.Fast, "true")]
    public void KnownSystems_ReturnDeclaredAxes(string name, int expectedX, int expectedY)
    {
        var (x, y) = DefaultLyapunovAxes.For(name);
        x.Should().Be(expectedX);
        y.Should().Be(expectedY);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void UnknownSystem_ReturnsDefault()
    {
        var (x, y) = DefaultLyapunovAxes.For("UnknownSystem");
        x.Should().Be(0);
        y.Should().Be(1);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void AllKnownSystems_HaveValidAxesWithinParameterRange()
    {
        foreach (var s in SystemCatalog.All)
        {
            if (s is not ChaoticSystem cs) continue;

            // Пропускаем системы с <2 параметрами — там карта Ляпунова
            // недоступна, и оси не имеют смысла.
            if (cs.Parameters.Length < 2) continue;

            var (x, y) = DefaultLyapunovAxes.For(cs.Name);
            int maxIdx = cs.Parameters.Length - 1;

            x.Should().BeInRange(0, maxIdx,
                $"{cs.Name}: x-axis out of range");
            y.Should().BeInRange(0, maxIdx,
                $"{cs.Name}: y-axis out of range");
        }
    }
}
