using ChaosLab.Core;

namespace ChaosLab.Tests;

/// <summary>ẋ = x → x(t) = x₀ · e^t. Точное решение для проверки RK4.</summary>
public sealed class ExponentialSystem : ChaoticSystem
{
    public override string Name => "Exponential";
    public override string Category => "Flow";
    public override int Dimension => 1;
    public override double[] DefaultInitialState => new[] { 1.0 };
    public override ParameterDef[] Parameters => [];

    public override void Derivatives(double[] s, double[] d, double[] p)
        => d[0] = s[0];

    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
        => r[0] = w[0];
}

/// <summary>ẍ = −x (гармонический осциллятор). Энергия E = (x² + v²)/2 сохраняется.</summary>
public sealed class HarmonicOscillator : ChaoticSystem
{
    public override string Name => "Harmonic";
    public override string Category => "Flow";
    public override int Dimension => 2;
    public override double[] DefaultInitialState => new[] { 1.0, 0.0 };
    public override ParameterDef[] Parameters => [];

    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        d[0] = s[1];
        d[1] = -s[0];
    }

    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        r[0] = w[1];
        r[1] = -w[0];
    }
}

/// <summary>Линейная карта x' = 2x. λ₁ = log 2 аналитически.</summary>
public sealed class LinearDoublingMap : ChaoticSystem
{
    public override string Name => "LinearDoubling";
    public override string Category => "Map";
    public override int Dimension => 1;
    public override bool IsDiscrete => true;
    public override double[] DefaultInitialState => new[] { 0.5 };
    public override ParameterDef[] Parameters => [];

    public override void Next(double[] s, double[] n, double[] p)
        => n[0] = 2.0 * s[0];

    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
        => r[0] = 2.0 * w[0];
}
