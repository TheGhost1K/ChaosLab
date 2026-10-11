using System.Collections.Concurrent;
using System.Collections.ObjectModel;

namespace ChaosLab.Systems;

/// <summary>
/// Каталог текстовых представлений уравнений систем.
/// </summary>
/// <remarks>
/// Используется UI для отображения формул в правой панели. Ключ — <see cref="Core.ISimulation.Name"/>.
/// </remarks>
public static class EquationsCatalog
{
    private static readonly ReadOnlyDictionary<string, string> _eq =
        new(new Dictionary<string, string>
        {
            ["Lorenz"] = "ẋ = σ(y − x)\nẏ = x(ρ − z) − y\nż = xy − βz",
            ["Rössler"] = "ẋ = −y − z\nẏ = x + ay\nż = b + z(x − c)",
            ["Aizawa"] = "ẋ = (z − b)x − dy\nẏ = dx + (z − b)y\nż = c + az − z³/3 − (x²+y²)(1+ez) + fzx³",
            ["Thomas"] = "ẋ = sin(y) − bx\nẏ = sin(z) − by\nż = sin(x) − bz",
            ["Halvorsen"] = "ẋ = −ax − 4y − 4z − y²\nẏ = −ay − 4z − 4x − z²\nż = −az − 4x − 4y − x²",
            ["Chen"] = "ẋ = a(y − x)\nẏ = (c − a)x − xz + cy\nż = xy − bz",
            ["Dadras"] = "ẋ = y − ax + byz\nẏ = cy − xz + z\nż = dxy − ez",
            ["Four-Wing"] = "ẋ = ax + yz\nẏ = bx + cy − xz\nż = −z − xy",
            ["Sprott A"] = "ẋ = y\nẏ = −x + yz\nż = 1 − y²",
            ["Rabinovich-Fabrikant"] = "ẋ = y(z − 1 + x²) + γx\nẏ = x(3z + 1 − x²) + γy\nż = −2z(α + xy)",
            ["Shimizu-Morioka"] = "ẋ = y\nẏ = x − λy − xz\nż = −αz + x²",
            ["Nosé-Hoover"] = "ẋ = y\nẏ = −x − yz\nż = y² − a",
            ["Langford"] = "ẋ = (ν − 1)x − y + xz\nẏ = x + (ν − 1)y + yz\nż = μz − (x²+y²+z²) − γz³",
            ["Lorenz-96"] = "ẋⱼ = (x_{j+1} − x_{j−2})x_{j−1} − xⱼ + F",
            ["Logistic"] = "x_{n+1} = r·xₙ(1 − xₙ)",
            ["Hénon"] = "x_{n+1} = 1 − a·xₙ² + yₙ\ny_{n+1} = b·xₙ",
            ["Ikeda"] = "x_{n+1} = 1 + u(xₙcos t − yₙ sin t)\ny_{n+1} = u(xₙ sin t + yₙ cos t)\nt = 0.4 − 6/(1 + xₙ² + yₙ²)",
            ["Clifford"] = "x_{n+1} = sin(ayₙ) + c·cos(axₙ)\ny_{n+1} = sin(bxₙ) + d·cos(byₙ)",
            ["De Jong"] = "x_{n+1} = sin(ayₙ) − cos(bxₙ)\ny_{n+1} = sin(cxₙ) − cos(dyₙ)",
            ["Tinkerbell"] = "x_{n+1} = xₙ² − yₙ² + axₙ + byₙ\ny_{n+1} = 2xₙyₙ + cxₙ + dyₙ",
            ["Gumowski-Mira"] = "x_{n+1} = byₙ + f(xₙ)\ny_{n+1} = −xₙ + f(xₙ)\nf(x) = ax + 2(1−a)x²/(1+x²)",
            ["Chirikov Standard"] = "p_{n+1} = pₙ + K·sin(qₙ)\nq_{n+1} = qₙ + p_{n+1}",
            ["Tent"] = "x_{n+1} = μ·xₙ       (xₙ < 0.5)\nx_{n+1} = μ(1 − xₙ)  (xₙ ≥ 0.5)",
            ["Gauss"] = "x_{n+1} = frac(α/xₙ + β)",
            ["Gray-Scott RD"] = "∂u/∂t = Du∇²u − uv² + F(1−u)\n∂v/∂t = Dv∇²v + uv² − (F+k)v",
            ["Chua's Circuit"] = "ẋ = α(y − x − g(x))\nẏ = x − y + z\nż = −βy\ng(x) = m₁x + ½(m₀−m₁)(|x+1| − |x−1|)",
            ["Rikitake Dynamo"] = "ẋ = −μx + yz\nẏ = −μy + (z − a)x\nż = 1 − xy",
            ["Newton-Leipnik"] = "ẋ = −ax + y + 10yz\nẏ = −x − 0.4y + 5xz\nż = bz − 5xy",
            ["Arnold Cat"] = "x_{n+1} = xₙ + yₙ (mod 1)\ny_{n+1} = xₙ + (1+k)·yₙ (mod 1)",
            ["Circle Map"] = "θ_{n+1} = θₙ + Ω − (K/2π)·sin(2π·θₙ) (mod 1)",
            ["Hénon-Heiles"] = "ẋ = pₓ\nẏ = p_y\nṗₓ = −x − 2xy\nṗ_y = −y − x² + y²",
            ["FPUT-β"] = "ẍᵢ = (xᵢ₊₁ − 2xᵢ + xᵢ₋₁) + β[(xᵢ₊₁−xᵢ)³ − (xᵢ−xᵢ₋₁)³]",
            ["Coupled Pendulums"] = "θ̈ᵢ = −sin θᵢ − k(2θᵢ − θᵢ₋₁ − θᵢ₊₁)",
        });

    private static readonly ConcurrentDictionary<string, string> _cache = new();

    /// <summary>
    /// Возвращает уравнения для системы. Результат кэшируется —
    /// повторные вызовы не пересобирают словарь.
    /// </summary>
    /// <param name="systemName">Имя системы.</param>
    /// <returns>Многострочная строка с уравнениями или пустая строка.</returns>
    public static string Get(string systemName)
        => _cache.GetOrAdd(systemName, n => _eq.TryGetValue(n, out var v) ? v : "");
}
