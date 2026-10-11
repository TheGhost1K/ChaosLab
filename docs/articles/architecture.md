# Архитектура

## Структура проекта

```text
ChaosLab/
├── src/ChaosLab/
│   ├── Core/
│   │   ├── ChaoticSystem.cs
│   │   ├── Camera3D.cs
│   │   ├── Palette.cs
│   │   └── SystemCatalog.cs
│   ├── Systems/
│   │   ├── Flows.cs
│   │   ├── FlowsExtended.cs
│   │   ├── HighDimSystems.cs
│   │   ├── Maps.cs
│   │   ├── MapsExtended.cs
│   │   ├── ReactionDiffusion.cs
│   │   └── EquationsCatalog.cs
│   ├── Analysis/
│   │   ├── LyapunovSpectrum.cs
│   │   ├── LyapunovMap.cs
│   │   ├── CorrelationDimension.cs
│   │   ├── BoxCounting.cs
│   │   ├── KaplanYorke.cs
│   │   ├── EntropyMeasures.cs
│   │   ├── HurstExponent.cs
│   │   ├── DetrendedFluctuation.cs
│   │   ├── ZeroOneTest.cs
│   │   ├── RecurrenceAnalysis.cs
│   │   ├── PoincareSection.cs
│   │   ├── BasinOfAttraction.cs
│   │   ├── PowerSpectrum.cs
│   │   ├── ReturnMap.cs
│   │   ├── EmbeddingDiagnostics.cs
│   │   └── Multifractal.cs
│   ├── Rendering/
│   │   ├── AttractorRenderer.cs
│   │   ├── BifurcationRenderer.cs
│   │   ├── CobwebRenderer.cs
│   │   ├── PlotRenderer.cs
│   │   └── VectorFieldRenderer.cs
│   ├── Services/
│   │   ├── RenderScheduler.cs
│   │   └── AnalysisService.cs
│   ├── ViewModels/
│   │   ├── BaseViewModel.cs
│   │   ├── RelayCommand.cs
│   │   ├── MainViewModel.cs
│   │   ├── SystemViewModel.cs
│   │   ├── CameraViewModel.cs
│   │   ├── RenderViewModel.cs
│   │   ├── AnalysisViewModel.cs
│   │   └── Converters.cs
│   ├── Views/
│   │   ├── MainWindow.xaml
│   │   └── MainWindow.xaml.cs
│   ├── App.xaml
│   ├── App.xaml.cs
│   └── ChaosLab.csproj
├── tests/ChaosLab.Tests/
├── docs/
├── .github/workflows/
├── ChaosLab.sln
├── README.md
├── LICENSE
├── CONTRIBUTING.md
├── .gitignore
├── .editorconfig
└── .gitattributes
```

## Паттерны

### ISimulation — общий контракт

Все вычислимые модели реализуют интерфейс:

```csharp
public interface ISimulation
{
    string Name { get; }
    string Category { get; }
    ParameterDef[] Parameters { get; }
}
```

Это позволяет UI обрабатывать потоки, отображения и поля единообразно.

### Асинхронный рендер с коалесцированием

`RenderScheduler` решает проблему «пользователь тащит слайдер → 60 запросов/sec».

Алгоритм:

1. `Request()` — если задача уже выполняется, ставит флаг `_pending`.
2. Если нет — запускает `RunAsync()`.
3. `RunAsync()` в цикле: берёт работу, отменяет предыдущую, возвращается.

Результат: выполняется только последний запрос. Промежуточные
отбрасываются — это правильное поведение для UI.

### Отмена через `IsCancellationRequested` в горячих циклах

В `Accumulate`, `BifurcationRenderer`, `LyapunovMap` НЕ используется
`ThrowIfCancellationRequested()` — только `if (ct.IsCancellationRequested) return;`.

Причина: `ThrowIfCancellationRequested` бросает `OperationCanceledException`
на каждый запрос отмены, а это:

- засоряет Output в VS
- срабатывает в debugger first-chance
- стоит ~1 µs на разворачивание стека

`IsCancellationRequested` — просто чтение volatile bool, JIT превращает
в одну инструкцию.

### Аналитический JacobianVectorProduct

Для каждой системы переопределён `JacobianVectorProduct`. Это даёт:

- ×2 ускорение для карты Ляпунова
- ×1.8 ускорение для спектра Ляпунова
- точность — нет нелинейных искажений от второй траектории

Fallback (численная directional derivative) работает автоматически для
новых систем, но медленнее.

### Параллелизм через `Parallel.For<TLocal>`

Все тяжёлые вычисления (бифуркации, карты, бассейны, корреляционная
размерность) распараллелены через `Parallel.ForEach` с локальными
буферами на поток:

```csharp
Parallel.ForEach(
    Partitioner.Create(0, height, chunk),
    () => new LocalScratch(dim),           // factory на поток
    (range, state, local) => { ... },      // body
    local => { /* merge в глобальный */ }  // finally
);
```

Ключевое: никаких блокировок в горячем цикле. Синхронизация только
на слиянии результатов.

## Потоки данных

```text
UI (WPF) ──Binding──> ViewModels ──Request/Cancel──> Scheduler
                                                          │
                                                     Task.Run
                                                          ▼
                                              Analysis/ + Rendering/
                                                          │
                                                     int[] pixels
                                                          ▼
                                              WriteableBitmap (UI thread)
```

## Что где искать

| Задача | Файл |
|---|---|
| Добавить систему | `Systems/Flows.cs` или `Maps.cs`, регистрация в `Core/SystemCatalog.cs` |
| Добавить метод анализа | `Analysis/НовыйМетод.cs`, вызов из `ViewModels/AnalysisViewModel.cs` |
| Изменить рендер | `Rendering/AttractorRenderer.cs` |
| Поправить UI | `Views/MainWindow.xaml` + `App.xaml` |
| Изменить палитру | `Core/Palette.cs` |
| Добавить горячую клавишу | `Views/MainWindow.xaml` (InputBindings) |
