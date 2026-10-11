namespace ChaosLab.Tests;

/// <summary>
/// Имена категорий тестов для фильтрации через dotnet test --filter.
/// </summary>
public static class TestCategories
{
    /// <summary>Быстрые тесты (&lt;1 сек). Запускаются всегда.</summary>
    public const string Fast = "Fast";

    /// <summary>Интеграционные тесты (1–10 сек).</summary>
    public const string Integration = "Integration";

    /// <summary>Долгие тесты для точных значений (&gt;10 сек). Только ночью.</summary>
    public const string Slow = "Slow";
}
