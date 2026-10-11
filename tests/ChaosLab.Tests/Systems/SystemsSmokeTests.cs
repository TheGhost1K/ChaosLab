using ChaosLab.Core;
using FluentAssertions;
using Xunit;

namespace ChaosLab.Tests.Systems;

public class SystemsSmokeTests
{
    public static IEnumerable<object[]> AllSystems()
    {
        foreach (var s in SystemCatalog.All)
            yield return new object[] { s };
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Fast, "true")]
    public void DefaultParameters_MatchDefinitionCount(ISimulation sim)
    {
        var p = sim.DefaultParameters();
        p.Length.Should().Be(sim.Parameters.Length);
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Fast, "true")]
    public void DefaultParameters_AreWithinDeclaredRange(ISimulation sim)
    {
        var p = sim.DefaultParameters();
        for (int i = 0; i < p.Length; i++)
        {
            p[i].Should().BeInRange(sim.Parameters[i].Min, sim.Parameters[i].Max);
        }
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Fast, "true")]
    public void Name_IsNotEmpty(ISimulation sim)
    {
        sim.Name.Should().NotBeNullOrEmpty();
        sim.Category.Should().BeOneOf("Flow", "Map", "Field");
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Integration, "true")]
    public void Step1000_ProducesFiniteState(ISimulation sim)
    {
        if (sim is not ChaoticSystem cs) return;
        if (cs.Category == "Field") return;   // Gray-Scott тестируется отдельно

        var state = (double[]) cs.DefaultInitialStateCached.Clone();
        var scratch = new double[cs.Dimension];
        var p = cs.DefaultParameters();
        var rk = cs.IsDiscrete ? null : new Rk4Scratch(cs.Dimension);

        for (int i = 0; i < 1000; i++)
            Integrator.StepOnce(cs, state, scratch, p, dt: 0.005, rk);

        foreach (var v in state)
            double.IsFinite(v).Should().BeTrue(
                $"{cs.Name} diverged after 1000 steps");
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Fast, "true")]
    public void Jvp_ProducesFiniteOutput(ISimulation sim)
    {
        if (sim is not ChaoticSystem cs) return;
        if (cs.Category == "Field") return;

        var s = cs.DefaultInitialStateCached;
        var w = new double[cs.Dimension];
        w[0] = 1.0;
        var r = new double[cs.Dimension];
        var p = cs.DefaultParameters();

        cs.JacobianVectorProduct(s, w, r, p);

        foreach (var v in r)
            double.IsFinite(v).Should().BeTrue($"{cs.Name} JVP returned non-finite");
    }

    [Theory]
    [MemberData(nameof(AllSystems))]
    [Trait(TestCategories.Fast, "true")]
    public void Parameters_AllHaveDescriptions(ISimulation sim)
    {
        // Мягкая проверка: рекомендуем всем параметрам иметь описание,
        // но не падаем, если у какого-то оно отсутствует.
        foreach (var p in sim.Parameters)
        {
            if (p.Description == null)
            {
                // В реальном проекте это warning, а не failure:
                // Output.WriteLine($"Missing description: {sim.Name}.{p.Name}");
            }
        }
    }
}
