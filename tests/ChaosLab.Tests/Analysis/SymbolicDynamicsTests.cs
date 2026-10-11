using ChaosLab.Analysis;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Analysis;

public class SymbolicDynamicsTests
{
    [Fact]
    public async Task LogisticAtR4_TopologicalEntropyNearLog2()
    {
        var sys = new LogisticMap();
        var res = await SymbolicDynamics.ComputeAsync(
            sys, new[] { 0.31 }, new[] { 4.0 },
            dt: 1.0, numSymbols: 4, sampleCount: 20_000,
            transient: 1000, coordIndex: 0);

        res.Should().NotBeNull();
        res!.TopologicalEntropy.Should().BeGreaterThan(0.3);
        res.ShannonEntropy.Should().BeGreaterThan(1.5);
    }

    [Fact]
    public async Task FixedPoint_ZeroEntropy()
    {
        // При r = 0.5 логистика сходится к 0
        var sys = new LogisticMap();
        var res = await SymbolicDynamics.ComputeAsync(
            sys, new[] { 0.5 }, new[] { 0.5 },
            dt: 1.0, numSymbols: 4, sampleCount: 5000,
            transient: 1000);

        res.Should().NotBeNull();
        res!.TopologicalEntropy.Should().BeLessThan(0.1);
    }
}
