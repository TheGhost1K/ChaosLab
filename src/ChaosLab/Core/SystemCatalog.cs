using ChaosLab.Systems;

namespace ChaosLab.Core;

/// <summary>
/// Центральный реестр всех вычислительных моделей, доступных в приложении.
/// </summary>
/// <remarks>
/// Единственное место, где регистрируются системы. Чтобы добавить новую —
/// создайте класс в <c>Systems/</c> и добавьте его экземпляр в <see cref="All"/>.
/// </remarks>
public static class SystemCatalog
{
    /// <summary>
    /// Полный список всех систем в порядке отображения в UI.
    /// </summary>
    public static readonly ISimulation[] All =
    {
        // --- Классические потоки ---
        new LorenzSystem(),
        new RosslerSystem(),
        new AizawaSystem(),
        new ThomasSystem(),
        new HalvorsenSystem(),
        new ChenSystem(),
        new DadrasSystem(),
        new FourWingSystem(),
        new SprottASystem(),

        // --- Новые потоки ---
        new ChuaCircuitSystem(),
        new HenonHeilesSystem(),
        new FputBetaSystem(),
        new CoupledPendulumChain(),
        new RikitakeDynamoSystem(),
        new NewtonLeipnikSystem(),
        new RabinovichFabrikantSystem(),
        new ShimizuMoriokaSystem(),
        new NoseHooverSystem(),
        new LangfordSystem(),
        new Lorenz96System(),

        // --- Отображения ---
        new LogisticMap(),
        new HenonMap(),
        new IkedaMap(),
        new CliffordMap(),
        new DeJongMap(),
        new TinkerbellMap(),

        // --- Новые отображения ---
        new ArnoldCatMap(),
        new CircleMap(),
        new GumowskiMiraMap(),
        new StandardMap(),
        new TentMap(),
        new GaussMap(),

        // --- Реакционно-диффузионные ---
        new GrayScottField(),
    };
}

/// <summary>
/// Умный выбор осей для карты Ляпунова по умолчанию.
/// Для каждой системы подбирает пару параметров, дающих наиболее
/// информативную картину перехода к хаосу.
/// </summary>
/// <remarks>
/// Значения — индексы в <see cref="ISimulation.Parameters"/>.
/// (X, Y) = (горизонтальная ось, вертикальная ось).
/// </remarks>
public static class DefaultLyapunovAxes
{
    /// <summary>
    /// Возвращает рекомендуемые оси для системы с указанным именем.
    /// </summary>
    /// <param name="systemName">Имя системы (<see cref="ISimulation.Name"/>).</param>
    /// <returns>Пара индексов (X, Y). Для неизвестных систем — (0, 1).</returns>
    public static (int x, int y) For(string systemName) => systemName switch
    {
        "Lorenz" => (1, 0),   // ρ по X, σ по Y
        "Chen" => (2, 0),   // c по X, a по Y
        "Rössler" => (2, 0),   // c по X, a по Y
        "Dadras" => (4, 0),   // e по X, a по Y
        "Aizawa" => (3, 1),   // d по X, b по Y
        "Halvorsen" => (0, 0),   // один параметр
        "Hénon" => (0, 1),   // a по X, b по Y
        "Clifford" => (0, 1),
        "De Jong" => (0, 1),
        "Tinkerbell" => (2, 3),   // c по X, d по Y
        "Gumowski-Mira" => (1, 2),
        "Shimizu-Morioka" => (0, 1),
        "Langford" => (0, 1),
        "Chua's Circuit" => (0, 1),   // α vs β
        "Rikitake Dynamo" => (0, 1),   // μ vs a
        "Newton-Leipnik" => (0, 1),   // a vs b
        _ => (0, 1),
    };
}
