# Разработка

## Как добавить новую систему

### 1. Создайте класс

Поместите его в `Systems/Flows.cs` (поток), `Systems/Maps.cs` (отображение)
или новый файл.

Пример потока:

```csharp
using System;
using ChaosLab.Core;

namespace ChaosLab.Systems;

/// <summary>
/// Система ВашаНазвание (год). Краткое описание.
/// </summary>
public sealed class YourSystem : ChaoticSystem
{
    public override string Name => "YourName";
    public override string Category => "Flow";
    public override int Dimension => 3;
    public override double[] DefaultInitialState => new[] { 0.1, 0.0, 0.0 };

    public override ParameterDef[] Parameters =>
    [
        new("α", 0.1, 2.0, 0.5),
        new("β", 0.1, 2.0, 0.5),
    ];

    public override (double, double, double, double, double, double) DefaultCamera
        => (0.5, -0.4, 40, 0, 0, 0);

    public override void Derivatives(double[] s, double[] d, double[] p)
    {
        double a = p[0], b = p[1];
        d[0] = /* ... */;
        d[1] = /* ... */;
        d[2] = /* ... */;
    }

    /// <summary>Аналитический JVP для скорости.</summary>
    public override void JacobianVectorProduct(
        double[] s, double[] w, double[] r, double[] p)
    {
        double a = p[0], b = p[1];
        // Заполняем r = J(s)·w
    }
}
```

### 2. Зарегистрируйте в каталоге

`Core/SystemCatalog.cs`:

```csharp
public static readonly ISimulation[] All =
{
    // ...
    new YourSystem(),
};
```

### 3. Добавьте уравнения в справку

`Systems/EquationsCatalog.cs`:

```csharp
["YourName"] = "ẋ = ...\nẏ = ...\nż = ...",
```

### 4. Укажите оси по умолчанию для карты Ляпунова

`Systems/SystemCatalog.cs`, класс `DefaultLyapunovAxes`:

```csharp
"YourName" => (0, 1),
```

### 5. Напишите тест

`tests/ChaosLab.Tests/SystemsTests.cs`:

```csharp
[Fact]
public void YourSystem_Lyapunov_Matches_Reference()
{
    var sys = new YourSystem();
    var spec = LyapunovSpectrum.ComputeAsync(
        sys, sys.DefaultInitialStateCached, sys.DefaultParameters(),
        0.001, 100_000).Result;

    Assert.InRange(spec[0], 0.10, 0.20);   // ожидаемое значение из литературы
}
```

## Как добавить метод анализа

### 1. Чистая функция в `Analysis/`

```csharp
using System.Threading;
using System.Threading.Tasks;
using ChaosLab.Core;

namespace ChaosLab.Analysis;

/// <summary>
/// Краткое описание метода и ссылка на статью.
/// </summary>
public static class MyMethod
{
    public static Task<double> ComputeAsync(
        ChaoticSystem sys, double[] initialState, double[] p,
        double dt, int steps,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // ...
            return 0.0;
        }, ct);
    }
}
```

### 2. Команда в `AnalysisViewModel`

```csharp
public RelayCommand MyMethodCommand { get; }

// В конструкторе:
MyMethodCommand = new RelayCommand(async () => await RunMyMethodAsync());

private async Task RunMyMethodAsync()
{
    if (Cs is not ChaoticSystem cs) return;
    NewCts(); Busy = true;
    try
    {
        var result = await MyMethod.ComputeAsync(
            cs, cs.DefaultInitialState, Params(), DtFor(cs),
            steps: 10000, ct: _cts!.Token);

        MyResultText = $"Результат: {result:F4}";
    }
    catch (OperationCanceledException) { }
    catch (Exception ex) { MyResultText = "Ошибка: " + ex.Message; }
    finally { Busy = false; }
}
```

### 3. Кнопка в XAML

Правая панель, вкладка «Анализ», блок «КОМАНДЫ»:

```xml
<Button Content="MyMethod"
        Command="{Binding Analysis.MyMethodCommand}"
        Style="{StaticResource AppButton}"/>
```

## Стиль кода

Проект следует конвенциям:

- 4 пробела для C#, 2 пробела для XAML
- LF для новых файлов
- `PascalCase` для публичных членов
- `_camelCase` для приватных полей
- Никаких `unsafe` и `stackalloc`
- Предпочитайте `Span<T>` и `ArrayPool<T>` для производительности
- Один публичный тип на файл (кроме маленьких helper'ов)

Включено через `.editorconfig`.

## Тестирование

```bash
dotnet test
```

Используется xUnit. Тесты делятся на:

- Unit — конкретные функции (`Palette.Sample`, `Integrator.Rk4Step`)
- Integration — сравнение с аналитическими значениями
  (`LyapunovSpectrum` для Лоренца)
- Regression — известные значения из литературы

Обязательные тесты для новой системы:

1. Значения по умолчанию дают хаос (λ₁ > 0)
2. Аналитический JVP совпадает с численным fallback (±1e-4)
3. Проверка инвариантов (сумма λ, D₂ и т.п.)

## Соглашения по коммитам

Используем [Conventional Commits](https://www.conventionalcommits.org/):

```text
<тип>(<область>): <краткое описание>

feat(systems): добавлена система Rabinovich-Fabrikant
fix(analysis): исправлена инвертированная ось Y в LyapunovMap
docs(readme): добавлен раздел про Gray-Scott
perf(rendering): параллельный BifurcationRenderer
```

Типы: `feat`, `fix`, `docs`, `refactor`, `perf`, `test`, `build`, `chore`.

Области: `core`, `systems`, `analysis`, `rendering`, `ui`, `docs`, `build`.

## Pull Request

Перед отправкой:

- [ ] `dotnet build` без warnings
- [ ] `dotnet test` — все тесты зелёные
- [ ] `.editorconfig` соблюдён
- [ ] Коммиты в стиле Conventional Commits
- [ ] Обновлены соответствующие `.md` в `docs/`
- [ ] Если новая система — добавлены в `SystemCatalog`, `EquationsCatalog`,
      `DefaultLyapunovAxes`, есть тест
