using ChaosLab.Core;
using ChaosLab.Systems;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class SimulationExtensionsTests
{
    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void DefaultParameters_MatchesManualExtraction()
    {
        var sim = new LorenzSystem();
        var viaExt = sim.DefaultParameters();

        var manual = new double[sim.Parameters.Length];
        for (int i = 0; i < manual.Length; i++)
            manual[i] = sim.Parameters[i].Default;

        viaExt.Should().Equal(manual);
    }

    [Fact]
    [Trait(TestCategories.Fast, "true")]
    public void DefaultParameters_ForEachSystem_LengthMatches()
    {
        foreach (var s in SystemCatalog.All)
            s.DefaultParameters().Length.Should().Be(s.Parameters.Length);
    }
}
