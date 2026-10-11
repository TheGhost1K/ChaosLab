---
_layout: landing
---

# API Reference

Полная документация по публичным типам ChaosLab, сгенерированная из
XML-комментариев. Все типы описаны с формулами, ссылками на первоисточники
и примерами использования.

## Быстрая навигация

### Базовые абстракции

- [ChaoticSystem](ChaosLab.Core.ChaoticSystem.html) — базовый класс для
  всех систем
- [ISimulation](ChaosLab.Core.ISimulation.html) — общий контракт
- [ISymplecticSystem](ChaosLab.Core.ISymplecticSystem.html) — интерфейс
  для гамильтоновых систем (Verlet)
- [Integrator](ChaosLab.Core.Integrator.html) — RK4, RK45, Verlet

### Системы

Все системы реализуют `ChaoticSystem` и зарегистрированы в
`SystemCatalog.All`:

- [ChaosLab.Systems](ChaosLab.Systems.html) — 33 вычислительные модели
- [LorenzSystem](ChaosLab.Systems.LorenzSystem.html)
- [ChuaCircuitSystem](ChaosLab.Systems.ChuaCircuitSystem.html)
- [HenonHeilesSystem](ChaosLab.Systems.HenonHeilesSystem.html)
- [SystemCatalog](ChaosLab.Core.SystemCatalog.html) — реестр всех систем

### Анализ

Методы нелинейного анализа находятся в
[ChaosLab.Analysis](ChaosLab.Analysis.html):

- [LyapunovSpectrum](ChaosLab.Analysis.LyapunovSpectrum.html) — спектр
  показателей Ляпунова
- [LyapunovMap](ChaosLab.Analysis.LyapunovMap.html) — карта λ₁(p₁, p₂)
- [CorrelationDimension](ChaosLab.Analysis.CorrelationDimension.html) — D₂
- [EntropyMeasures](ChaosLab.Analysis.EntropyMeasures.html) — SampEn, PermEn
- [KolmogorovSinaiEntropy](ChaosLab.Analysis.KolmogorovSinaiEntropy.html) — h_KS

### Рендеринг

- [ChaosLab.Rendering](ChaosLab.Rendering.html)
- [AttractorRenderer](ChaosLab.Rendering.AttractorRenderer.html)
- [BifurcationRenderer](ChaosLab.Rendering.BifurcationRenderer.html)
- [PlotRenderer](ChaosLab.Rendering.PlotRenderer.html)

### MVVM

- [ChaosLab.ViewModels](ChaosLab.ViewModels.html)
- [RenderViewModel](ChaosLab.ViewModels.RenderViewModel.html)
- [AnalysisViewModel](ChaosLab.ViewModels.AnalysisViewModel.html)

## Как читать документацию

Каждый публичный тип документирован через XML-комментарии и включает:

- **Summary** — что делает
- **Remarks** — формулы, детали реализации, ссылки на первоисточники
- **Parameters** — описание каждого параметра
- **Returns** — что возвращает
- **Exceptions** — какие исключения может бросать
- **See Also** — ссылки на связанные типы и внешние статьи

## Пример использования

```csharp
using ChaosLab.Core;
using ChaosLab.Analysis;
using ChaosLab.Systems;

// Получить систему
var lorenz = new LorenzSystem();

// Вычислить спектр Ляпунова
var spectrum = await LyapunovSpectrum.ComputeAsync(
    lorenz,
    lorenz.DefaultInitialStateCached,
    lorenz.DefaultParameters(),
    dt: 0.001,
    steps: 200_000);

Console.WriteLine($"λ₁ = {spectrum[0]:F4}");  // ≈ +0.906
```

## Полная документация

- [Начало работы](../articles/getting-started.md)
- [Системы](../articles/systems/index.md)
- [Методы анализа](../articles/methods/index.md)
- [Архитектура](../articles/architecture.md)

## Версии

- **1.0.0** — первый публичный релиз
- .NET 10, WPF, Windows 10/11

См. [CHANGELOG](https://github.com/TheGhost1K/ChaosLab/blob/master/CHANGELOG.md)
для полной истории.