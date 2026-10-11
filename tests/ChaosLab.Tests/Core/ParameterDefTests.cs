using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class ParameterDefTests
{
    [Fact]
    public void Step_IsRangeOver500()
    {
        var p = new ParameterDef("σ", 0, 10, 5);
        p.Step.Should().BeApproximately(10.0 / 500.0, 1e-12);
    }

    [Fact]
    public void RecordEquality_Works()
    {
        var a = new ParameterDef("x", 0, 1, 0.5);
        var b = new ParameterDef("x", 0, 1, 0.5);
        a.Should().Be(b);
    }

    [Fact]
    public void Description_DefaultsToNull()
    {
        var p = new ParameterDef("σ", 0, 10, 5);
        p.Description.Should().BeNull();
    }

    [Fact]
    public void Description_CanBeSet()
    {
        var p = new ParameterDef("σ", 0, 10, 5, "Число Прандтля");
        p.Description.Should().Be("Число Прандтля");
    }

    [Fact]
    public void RecordEquality_IgnoresNothing()
    {
        var a = new ParameterDef("x", 0, 1, 0.5, "desc");
        var b = new ParameterDef("x", 0, 1, 0.5, "desc");
        a.Should().Be(b);
    }

    [Fact]
    public void RecordEquality_DifferentDescriptions_AreDifferent()
    {
        var a = new ParameterDef("x", 0, 1, 0.5, "A");
        var b = new ParameterDef("x", 0, 1, 0.5, "B");
        a.Should().NotBe(b);
    }
}
