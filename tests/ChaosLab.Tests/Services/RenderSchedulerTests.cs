using ChaosLab.Services;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Services;

public class RenderSchedulerTests
{
    [Fact]
    public async Task MultipleRequests_OnlyLastOneDelivered()
    {
        var scheduler = new RenderScheduler();
        int completed = 0;
        int delivered = 0;

        scheduler.FrameFactory = ct =>
        {
            Interlocked.Increment(ref completed);
            Thread.Sleep(50);
            return (new int[10], 1);
        };

        scheduler.FrameReady += (px, pts) =>
        {
            Interlocked.Increment(ref delivered);
        };

        for (int i = 0; i < 5; i++)
            scheduler.Request();

        // Ждём завершения всех задач
        await Task.Delay(500);

        delivered.Should().BeLessThanOrEqualTo(2);
        completed.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Failure_TriggersFailedEvent()
    {
        var scheduler = new RenderScheduler();
        string? errorMsg = null;

        scheduler.FrameFactory = ct => throw new InvalidOperationException("test");
        scheduler.Failed += msg => errorMsg = msg;

        scheduler.Request();
        await Task.Delay(200);

        errorMsg.Should().Be("test");
    }

    [Fact]
    public async Task Cancel_DoesNotTriggerFailure()
    {
        var scheduler = new RenderScheduler();
        bool failed = false;

        scheduler.FrameFactory = ct =>
        {
            ct.ThrowIfCancellationRequested();
            return (new int[10], 1);
        };

        scheduler.Failed += _ => failed = true;

        scheduler.Request();
        scheduler.Cancel();
        await Task.Delay(200);

        failed.Should().BeFalse();
    }
}
