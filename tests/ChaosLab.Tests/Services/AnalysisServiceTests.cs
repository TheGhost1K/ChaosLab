using ChaosLab.Services;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Services;

public class AnalysisServiceTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task RunAsync_ExecutesWork()
    {
        using var svc = new AnalysisService();

        var result = await svc.RunAsync(ct => 42);

        result.Should().Be(42);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public async Task StartNew_CancelsPrevious()
    {
        using var svc = new AnalysisService();
        var firstToken = svc.StartNew();
        var secondToken = svc.StartNew();

        firstToken.IsCancellationRequested.Should().BeTrue();
        secondToken.IsCancellationRequested.Should().BeFalse();
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Cancel_RequestsCancellation()
    {
        using var svc = new AnalysisService();
        var token = svc.StartNew();
        svc.Cancel();
        token.IsCancellationRequested.Should().BeTrue();
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void Dispose_DoesNotThrow()
    {
        var svc = new AnalysisService();
        svc.StartNew();
        svc.Dispose();
    }
}
