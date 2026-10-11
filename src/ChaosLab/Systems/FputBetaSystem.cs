using System;
using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Цепочка Ферми–Паста–Улама–Цингу (FPUT-β, 1955) — нелинейная
/// одномерная решётка с кубической и квадратичной нелинейностью.
/// </summary>
/// <remarks>
/// <para>Гамильтониан (фиксированные границы q₀ = q_{N+1} = 0):</para>
/// <code>
/// H = Σᵢ pᵢ²/2 + Σᵢ [ ½(qᵢ₊₁ − qᵢ)² + (β/4)·(qᵢ₊₁ − qᵢ)⁴ ]
/// </code>
/// <para>Классический эксперимент 1955 года: небольшая ангармоническая
/// поправка **не** приводит к равномерному распределению энергии по модам,
/// как ожидалось. Вместо этого энергия почти полностью возвращается в
/// первую моду через характерное время T_рекуррентности — знаменитое
/// «открытие FPUT».</para>
/// <para>При больших β и N ≈ 8+ система хаотична на коротких временах,
/// но сохраняет квазипериодическую рекуррентность на больших.</para>
/// </remarks>
/// <seealso href="https://doi.org/10.1063/1.882934"/>
public sealed class FputBetaSystem : ChaoticSystem, ISymplecticSystem
{
    private int _n = 8;
    private double[] _state = Array.Empty<double>();

    /// <summary>Число частиц в цепочке (N). Размерность состояния = 2N.</summary>
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
    public override string Name => "FPUT-β";
    /// <inheritdoc/>
    public override string Category => "Flow";
    /// <inheritdoc/>
    public override int Dimension => 2 * _n;
    /// <inheritdoc/>
    public override double[] DefaultInitialState => _state;

    /// <inheritdoc/>
    public int HalfDimension => _n;

    /// <summary>Инициализирует систему с первой модой.</summary>
    public FputBetaSystem()
    {
        RegenerateInitialState();
    }

    private void RegenerateInitialState()
    {
        _state = new double[2 * _n];
        // Первая мода: qᵢ = A·sin(π·i/(N+1))
        // Амплитуда 0.5 — компромисс между видимым движением и
        // численной устойчивостью RK4 при dt по умолчанию 0.005.
        const double amplitude = 0.5;
        for (int i = 0; i < _n; i++)
            _state[i] = amplitude * Math.Sin(Math.PI * (i + 1) / (_n + 1));
        InvalidateCachedInitialState();
    }

    /// <inheritdoc/>
    public override ParameterDef[] Parameters =>
    [
        new("β", 0.05, 2.0, 1.0,
            "Коэффициент нелинейности. β = 0 — линейная цепочка (нет хаоса). " +
            "β > 0.5 — хаос в узкой полосе энергий."),
        new("N", 4, 16, 8,
            "Число частиц. Больше N — богаче спектр мод, легче хаос."),
    ];

    /// <inheritdoc/>
    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.3, 8, 0, 0, 0);

    /// <inheritdoc/>
    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        int n = _n;
        double beta = p[0];

        // d qᵢ/dt = pᵢ
        for (int i = 0; i < n; i++) d[i] = s[n + i];

        // d pᵢ/dt = (qᵢ₊₁ − qᵢ) + β(qᵢ₊₁ − qᵢ)³ − (qᵢ − qᵢ₋₁) − β(qᵢ − qᵢ₋₁)³
        for (int i = 0; i < n; i++)
        {
            double left = (i == 0) ? 0.0 : s[i - 1];
            double right = (i == n - 1) ? 0.0 : s[i + 1];

            double dr = right - s[i];
            double dl = s[i] - left;

            d[n + i] = dr + beta * dr * dr * dr
                     - dl - beta * dl * dl * dl;
        }
    }

    /// <inheritdoc/>
    public void GradientPotential(ReadOnlySpan<double> q, Span<double> gradV, double[] p)
    {
        int n = _n;
        double beta = p[0];

        for (int i = 0; i < n; i++)
        {
            double left = (i == 0) ? 0.0 : q[i - 1];
            double right = (i == n - 1) ? 0.0 : q[i + 1];

            double dr = right - q[i];
            double dl = q[i] - left;

            // gradV[i] = ∂V/∂qᵢ = −F_i
            gradV[i] = -dr - beta * dr * dr * dr
                     + dl + beta * dl * dl * dl;
        }
    }

    /// <summary>
    /// Аналитический JVP. Матрица имеет трёхдиагональную структуру с
    /// постоянной диагональю.
    /// </summary>
    public override void JacobianVectorProduct(double[] s, double[] w, double[] r, double[] p)
    {
        int n = _n;
        double beta = p[0];

        // Верхняя половина (q): ∂q̇ᵢ/∂pⱼ = δᵢⱼ
        for (int i = 0; i < n; i++) r[i] = w[n + i];

        // Нижняя половина (p): ∂ṗᵢ/∂qⱼ — трёхдиагональная
        for (int i = 0; i < n; i++)
        {
            double left = (i == 0) ? 0.0 : s[i - 1];
            double right = (i == n - 1) ? 0.0 : s[i + 1];

            double dr = right - s[i];
            double dl = s[i] - left;

            // Диагональный элемент: d/dqᵢ (dr + βdr³ − dl − βdl³)
            //   = (−1 − 3β dr²) − (1 + 3β dl²) = −2 − 3β(dr² + dl²)
            double diag = -2.0 - 3.0 * beta * (dr * dr + dl * dl);

            double lower = 0, upper = 0;
            if (i > 0)
            {
                double dl_next = s[i] - s[i - 1];
                lower = 1.0 + 3.0 * beta * dl_next * dl_next;
            }
            if (i < n - 1)
            {
                double dr_next = s[i + 1] - s[i];
                upper = 1.0 + 3.0 * beta * dr_next * dr_next;
            }

            r[n + i] = diag * w[i] + lower * (i > 0 ? w[i - 1] : 0)
                                   + upper * (i < n - 1 ? w[i + 1] : 0);
        }
    }

    /// <inheritdoc/>
    public override void Project3D(double[] s, Span<double> xyz)
    {
        xyz[0] = s.Length > 0 ? s[0] : 0;
        xyz[1] = s.Length > 1 ? s[1] : 0;
        xyz[2] = s.Length > 2 ? s[2] : 0;
    }
}
