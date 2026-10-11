using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Core;

public class SystemCatalogTests
{
    [Fact]
    public void All_ContainsAtLeastTwentySystems()
    {
        SystemCatalog.All.Length.Should().BeGreaterThanOrEqualTo(20);
    }

    [Fact]
    public void All_SystemNamesAreUnique()
    {
        var names = SystemCatalog.All.Select(s => s.Name).ToArray();
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void EverySystem_HasDefaultParametersMatchingDefs()
    {
        foreach (var sys in SystemCatalog.All)
        {
            if (sys is ChaoticSystem cs)
            {
                var p = cs.DefaultParameters();
                p.Length.Should().Be(cs.Parameters.Length);
            }
        }
    }

    [Fact]
    public void EveryChaoticSystem_HasFiniteInitialState()
    {
        foreach (var sys in SystemCatalog.All)
        {
            if (sys is ChaoticSystem cs)
            {
                foreach (var v in cs.DefaultInitialState)
                    double.IsFinite(v).Should().BeTrue($"{cs.Name} has non-finite initial state");
            }
        }
    }

    [Fact]
    public void EveryChaoticSystem_JacobianVectorProductProducesFinite()
    {
        foreach (var sys in SystemCatalog.All)
        {
            if (sys is not ChaoticSystem cs) continue;
            if (cs.IsDiscrete) continue;
            if (cs.Category == "Field") continue;

            var s = cs.DefaultInitialStateCached;
            var w = new double[cs.Dimension];
            w[0] = 1;
            var r = new double[cs.Dimension];
            var p = cs.DefaultParameters();

            cs.JacobianVectorProduct(s, w, r, p);

            foreach (var v in r)
                double.IsFinite(v).Should().BeTrue($"{cs.Name} JVP produced non-finite");
        }
    }
}
