# Методы анализа

ChaosLab включает **24 метода** нелинейного анализа.

## Категории

### Спектральные характеристики

- [Показатели Ляпунова](lyapunov.md) — скорость расхождения траекторий
- [Kolmogorov-Sinai entropy](ks-entropy.md) — сумма положительных λ₁
- [Power Spectrum](power-spectrum.md) — частотный состав
- [Резонансный анализ](resonance-analysis.md) — рациональные соотношения

### Фрактальные размерности

- [Корреляционная размерность D₂](correlation-dimension.md)
- [Box-counting](box-counting.md) - D_box
- [Kaplan-Yorke](kaplan-yorke.md) - D_KY
- [Higuchi FD](higuchi-fd.md) — размерность временного ряда
- [Multifractal D_q](multifractal.md) — обобщённый спектр

### Энтропии

- [Sample Entropy](sample-entropy.md) — непредсказуемость
- [Approximate Entropy](approximate-entropy.md) — предшественник SampEn
- [Permutation Entropy](permutation-entropy.md) — сложность ординальных паттернов
- [Multiscale Entropy](multiscale-entropy.md) — энтропия по масштабам
- [Hurst exponent](hurst-exponent.md) — персистентность
- [DFA](dfa.md) — устойчивый аналог Hurst

### Алгоритмическая сложность

- [Lempel–Ziv complexity](lempel-ziv.md) — комбинаторная сложность
- [Символьная динамика](symbolic-dynamics.md) — топологическая энтропия

### Тесты на хаос

- [0–1 тест](zero-one-test.md) — K ≈ 1 для хаоса
- [RQA](rqa.md) — анализ матрицы возвратов

### Предсказуемость

- [Predictability horizon](predictability.md) — практический предел
- [Basin entropy](basin-entropy.md) — сложность бассейнов

### Геометрические структуры

- [Сечения Пуанкаре](poincare-section.md)
- [Бассейны притяжения](basin-of-attraction.md)
- [Return map](return-map.md)

## Что выбрать для задачи

| Задача | Метод |
|--------|-------|
| «Есть ли хаос?» | λ₁, 0–1 тест, h_top |
| «Насколько сложный?» | D₂, PermEn, MSE, LZ |
| «Можно ли предсказать?» | λ₁, SampEn, Hurst, Predictability |
| «Какая структура?» | Сечения Пуанкаре, RQA |
| «Фрактал или нет?» | D₂ vs D_box, Multifractal, Higuchi |
| «Тор или хаос?» | Resonance + D₂ |
| «Гиперхаос?» | h_KS, PositiveExponentCount |
| «Сколько режимов?» | Basin Entropy |

## Общий принцип

Все методы в ChaosLab:

1. **Не блокируют UI** — считаются в фоновом потоке.
2. **Отменяемы** — кнопка «Отмена» прерывает за миллисекунды.
3. **Проверены на эталонных системах** — тесты в `tests/`.
4. **Имеют ссылку на первоисточник** — в XML-комментариях.
