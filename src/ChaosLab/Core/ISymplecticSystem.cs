namespace ChaosLab.Core;

/// <summary>
/// Интерфейс для гамильтоновых систем вида <c>H(q, p) = T(p) + V(q)</c>,
/// для которых применим симплектический интегратор Verlet.
/// </summary>
/// <remarks>
/// <para>Размерность <see cref="ChaoticSystem.Dimension"/> должна быть
/// чётной: первые <see cref="HalfDimension"/> компонент — координаты q,
/// последние — импульсы p.</para>
/// <para>Уравнения движения:</para>
/// <code>
/// q̇ = ∂T/∂p
/// ṗ = −∂V/∂q
/// </code>
/// <para>Для кинетической энергии вида <c>T = ½·Σpᵢ²</c> имеем
/// <c>q̇ = p</c>, и достаточно знать только <c>∇V(q)</c>.</para>
/// </remarks>
public interface ISymplecticSystem
{
    /// <summary>Половина размерности: N для N-мерных q и p.</summary>
    int HalfDimension { get; }

    /// <summary>
    /// Градиент потенциальной энергии в точке q:
    /// <c>gradV[i] = ∂V/∂qᵢ</c>.
    /// </summary>
    /// <param name="q">Координаты (длина <see cref="HalfDimension"/>).</param>
    /// <param name="gradV">Результат: градиент.</param>
    /// <param name="p">Параметры системы.</param>
    void GradientPotential(ReadOnlySpan<double> q, Span<double> gradV, double[] p);
}

/// <summary>
/// Переиспользуемые буферы для интегратора Verlet.
/// </summary>
public sealed class VerletScratch
{
    /// <summary>Полушаг импульсов.</summary>
    public readonly double[] HalfP;
    /// <summary>Градиент потенциальной энергии.</summary>
    public readonly double[] GradV;

    /// <summary>Создаёт буферы под половину размерности N.</summary>
    public VerletScratch(int halfN)
    {
        HalfP = new double[halfN];
        GradV = new double[halfN];
    }
}
