using System;
using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Цепочка связанных математических маятников с пружинной связью.
/// Гамильтонова система, поддерживает симплектический интегратор Verlet.
/// </summary>
/// <remarks>
/// <para>Гамильтониан (m = l = g = 1):</para>
/// <code>
/// H = Σᵢ pᵢ²/2 + Σᵢ (1 − cos θᵢ) + (k/2)·Σᵢ (θᵢ₊₁ − θᵢ)²
/// </code>
/// <para>При k = 0 — N независимых маятников (интегрируемо).
/// При k &gt; 0 — связь, приводящая к хаосу для больших амплитуд.</para>
/// <para>Граничные условия: θ₀ = θ_{N+1} = 0 (закреплённые концы).</para>
/// <para>Угол θ измеряется от положения равновесия вниз.</para>
/// </remarks>
public sealed class CoupledPendulumChain : ChaoticSystem, ISymplecticSystem
{
    private int _n = 6;
    private double[] _state = Array.Empty<double>();

    /// <summary>Число маятников в цепочке.</summary>
    public int N
    {
        get => _n;
        set
        {
            if (value < 2) value = 2;
            if (value == _n) return;
            _n = value;
            RegenerateInitialState();
        }
    }

    /// <inheritdoc/>
    public override string Name => "Coupled Pendulums";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 2 * _n;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => _state;

    /// <inheritdoc/>
    public int HalfDimension => _n;

    /// <summary>Инициализирует систему: первый маятник отклонён, остальные — в равновесии.</summary>
    public CoupledPendulumChain()
    {
        RegenerateInitialState();
    }

    private void RegenerateInitialState()
    {
        _state = new double[2 * _n];
        // θ₁ = 1 рад (57°) — достаточно для нелинейности,
        // но не настолько, чтобы RK4/Verlet при dt = 0.005 «взрывались».
        _state[0] = 1.0;
        InvalidateCachedInitialState();
    }

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("k", 0.1, 3.0, 1.0,
            "Жёсткость пружин связи. k = 0 — независимые маятники. " +
            "k > 0.5 при больших амплитудах — хаос."),
        new("N", 3, 10, 6,
            "Число маятников. Больше N — богаче динамика."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.3, 20, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        int n = _n;
        double k = p[0];

        for (int i = 0; i < n; i++) d[i] = s[n + i];

        for (int i = 0; i < n; i++)
        {
            double theta_i = s[i];
            double left = (i == 0) ? 0.0 : s[i - 1];
            double right = (i == n - 1) ? 0.0 : s[i + 1];

            // ṗᵢ = −sin θᵢ − k(2θᵢ − θᵢ₋₁ − θᵢ₊₁)
            d[n + i] = -Math.Sin(theta_i)
                     - k * (2.0 * theta_i - left - right);
        }
    }

    /// <inheritdoc/>
    public void GradientPotential(ReadOnlySpan<double> q, Span<double> gradV, double[] p)
    {
        int n = _n;
        double k = p[0];

        for (int i = 0; i < n; i++)
        {
            double theta_i = q[i];
            double left = (i == 0) ? 0.0 : q[i - 1];
            double right = (i == n - 1) ? 0.0 : q[i + 1];

            gradV[i] = Math.Sin(theta_i) + k * (2.0 * theta_i - left - right);
        }
    }

    /// <summary>
    /// Аналитический JVP. Якобиан имеет блочную структуру:
    /// верхний левый блок — 0, верхний правый — I,
    /// нижний левый — трёхдиагональный (−cos θᵢ на диагонали, k вне диагонали),
    /// нижний правый — 0.
    /// </summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        int n = _n;
        double k = p[0];

        // Верхняя половина: dq̇ᵢ/dpⱼ = δᵢⱼ
        for (int i = 0; i < n; i++) r[i] = w[n + i];

        // Нижняя половина
        for (int i = 0; i < n; i++)
        {
            double cosTheta = Math.Cos(s[i]);
            double diag = -cosTheta - 2.0 * k;

            double val = diag * w[i];
            if (i > 0) val += k * w[i - 1];
            if (i < n - 1) val += k * w[i + 1];

            r[n + i] = val;
        }
    }

    /// <inheritdoc/>
    public override void Project3D(double[] s, Span<double> xyz)
    {
        // Проекция на первые три угла
        xyz[0] = s.Length > 0 ? s[0] : 0;
        xyz[1] = s.Length > 1 ? s[1] : 0;
        xyz[2] = s.Length > 2 ? s[2] : 0;
    }
}
