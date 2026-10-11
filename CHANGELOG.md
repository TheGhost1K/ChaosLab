# Changelog

Все значимые изменения проекта документируются в этом файле.

Формат основан на [Keep a Changelog](https://keepachangelog.com/ru/1.1.0/),
версии соответствуют [Semantic Versioning](https://semver.org/lang/ru/).

## [Unreleased]

### Планируется
- Presets library
- Multi-panel view (2×2)

## [1.0.0] — 2026-10-11

Первый публичный релиз.

### Добавлено
- 33 вычислительные модели: 20 потоков, 12 отображений, Gray-Scott
- 10 режимов отображения (Attractor, Bifurcation, Lyapunov Map, Cobweb,
  Power Spectrum, Return Map, FNN+MI, Multifractal, Multiscale Entropy, Trail)
- 24 метода анализа (Lyapunov, KS entropy, D₂, Higuchi FD, SampEn, ApEn,
  PermEn, LZ, RQA, Symbolic, Resonance, MSE, Basin Entropy, Predictability…)
- Аналитические якобианы для всех систем (×2 ускорение)
- Параллельные карты Ляпунова и бассейны (×6–8)
- Прогрессивный рендер карты Ляпунова (превью 256×256 за 0.3 с)
- Adaptive RK45 и Verlet интеграторы
- Vector field overlay для 2D отображений
- 351+ unit-тестов, покрытие ~88%
- Полная документация DocFX
- CI на GitHub Actions
