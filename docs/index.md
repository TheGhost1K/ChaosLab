# ChaosLab

**ChaosLab** — десктопное WPF-приложение для исследования
детерминированного хаоса. **33 вычислительные модели**, **10 режимов
отображения**, **24 метода нелинейного анализа** в одном инструменте.

## С чего начать

- [Начало работы](articles/getting-started.md) — установка, интерфейс,
  первый аттрактор за 30 секунд
- [Системы](articles/systems/index.md) — все 33 модели с уравнениями
- [Методы анализа](articles/methods/index.md) — как измерить хаос
- [Архитектура](articles/architecture.md) — как устроен код

## Быстрые ссылки

### Классические системы

- [Lorenz](articles/systems/lorenz.md) — «эффект бабочки», λ₁ ≈ 0.906
- [Rössler](articles/systems/rossler.md) — ленточный аттрактор
- [Chua's Circuit](articles/systems/chua.md) — первая электронная
  схема с хаосом
- [Hénon-Heiles](articles/systems/henon-heiles.md) — гамильтонов хаос,
  поддержка Verlet
- [Lorenz-96](articles/systems/lorenz-96.md) — N-мерная модель,
  гиперхаос
- [Gray-Scott](articles/systems/gray-scott.md) — реакционно-диффузионные
  паттерны

### Отображения

- [Logistic](articles/systems/logistic.md) — каскад Фейгенбаума
- [Hénon](articles/systems/henon.md) — D₂ ≈ 1.26
- [Arnold Cat](articles/systems/arnold-cat.md) — λ₁ = log φ² аналитически
- [Circle Map](articles/systems/circle-map.md) — языки Арнольда

### Методы анализа

- [Показатели Ляпунова](articles/methods/lyapunov.md) — измерение хаоса
- [KS Entropy](articles/methods/ks-entropy.md) — сумма положительных λᵢ
- [Корреляционная размерность](articles/methods/correlation-dimension.md) — D₂
- [Kaplan-Yorke](articles/methods/kaplan-yorke.md) — D_KY из спектра
- [Higuchi FD](articles/methods/higuchi-fd.md) — размерность временного ряда
- [Sample Entropy](articles/methods/sample-entropy.md) — непредсказуемость
- [Permutation Entropy](articles/methods/permutation-entropy.md) — сложность паттернов
- [Lempel-Ziv](articles/methods/lempel-ziv.md) — алгоритмическая сложность
- [Символьная динамика](articles/methods/symbolic-dynamics.md) — топологическая энтропия
- [Predictability](articles/methods/predictability.md) — горизонт предсказуемости
- [Все методы](articles/methods/index.md)

### Интеграторы

- [Adaptive Integrators](articles/methods/adaptive-integrators.md) — RK45 с
  контролем ошибки
- [Symplectic Integrators](articles/methods/symplectic-integrators.md) —
  Verlet для гамильтоновых систем

## Возможности

### Системы

| Категория | Кол-во | Примеры |
|---|---|---|
| Потоки (Flow) | 20 | Lorenz, Rössler, Chua, Hénon-Heiles, FPUT-β |
| Отображения (Map) | 12 | Logistic, Hénon, Arnold Cat, Circle, Ikeda |
| Поля (Field) | 1 | Gray-Scott |
| **Всего** | **33** | |

### Режимы отображения

| Режим | Что показывает |
|---|---|
| **Attractor** | Фазовый портрет |
| **Bifurcation** | Диаграмма по параметру |
| **Lyapunov Map** | Тепловая карта λ₁ (p₁, p₂) |
| **Cobweb** | Паутина итераций (1D) |
| **Power Spectrum** | Спектр через FFT |
| **Return Map** | xₙ → xₙ₊₁ |
| **FNN + MI** | Диагностика вложения |
| **Multifractal** | Спектр D_q |
| **Multiscale Entropy** | SampEn(τ) |
| **Trail** | Живой след траектории |

### Методы анализа

- **Спектральные** — Lyapunov spectrum, KS entropy, Power spectrum,
  Resonance
- **Размерности** — D₂, D_box, D_KY, Higuchi FD, Multifractal
- **Энтропии** — SampEn, ApEn, PermEn, MSE, Hurst, DFA
- **Сложность** — Lempel-Ziv, Symbolic dynamics
- **Тесты на хаос** — 0–1 test, RQA
- **Геометрия** — Poincaré section, Basins of attraction, Return map
- **Предсказуемость** — Basin entropy, Predictability horizon

### Численные методы

- **RK4** — классический Рунге–Кутта 4-го порядка (по умолчанию)
- **Adaptive RK45** — Dormand-Prince с контролем ошибки
- **Verlet** — симплектический интегратор для гамильтоновых систем
  (Hénon-Heiles, FPUT-β, Coupled Pendulums)

### Технические особенности

- **Аналитические якобианы** — вдвое быстрее численной аппроксимации
- **Параллельные карты Ляпунова** — ×6–8 на типичном CPU
- **Прогрессивный рендер** — превью 256×256 за 0.3 с
- **Асинхронный планировщик** — UI никогда не блокируется
- **Debounce слайдеров** — рендер перезапускается с задержкой
- **351+ unit-тестов** — покрытие ~88%
- **Полная документация** — 33 статьи систем + 27 статей методов

## Примеры использования

### Построить классический аттрактор Лоренца

1. Система: **Lorenz** → Attractor
2. Палитра: **Inferno**, Gamma: 0.85
3. Iterations: 5M → Render

### Найти границу перехода к хаосу

1. Система: **Lorenz** → Lyapunov Map
2. Ось X: ρ `[1, 60]`, ось Y: σ `[1, 30]`
3. Белая линия — граница λ = 0
4. **Правый клик** по интересной точке → переход в Attractor

### Сравнить RK4 и Verlet

1. Система: **Hénon-Heiles**
2. Включить **Use Verlet integrator** → Render
3. Выключить → Render
4. Разница видна на длинных прогонах: RK4 «уползает», Verlet сохраняет
   энергию

### Построить живую траекторию

1. Любая система → режим **Trail**
2. Кнопка **Animate** → след рисуется в реальном времени
3. Настройте `TrailLength` для длинного/короткого хвоста

### Найти два аттрактора

1. Система: **Newton-Leipnik**
2. Начальное состояние `(0.349, 0, −0.160)` → аттрактор A
3. Изменить на `(−0.349, 0, 0.160)` → аттрактор B
4. Сравнить структуры

## Что нового в 1.0

- 33 системы (было 25): добавлены Chua, Rikitake, Newton-Leipnik,
  Hénon-Heiles, FPUT-β, Coupled Pendulums, Arnold Cat, Circle Map
- 24 метода анализа (было 18): добавлены ApEn, LZ, Higuchi, KS entropy,
  Basin entropy, Predictability
- 10 режимов (было 9): добавлен Trail
- Adaptive RK45 и Verlet интеграторы
- Vector field overlay для 2D отображений
- Полная документация DocFX

## Вклад

См. [CONTRIBUTING.md](https://github.com/TheGhost1K/ChaosLab/blob/master/CONTRIBUTING.md).
Мы рады:

- новым системам (присылайте с проверкой λ)
- новым методам анализа
- оптимизациям
- улучшениям UI

## Лицензия

MIT — см. [LICENSE](https://github.com/TheGhost1K/ChaosLab/blob/master/LICENSE).

## Благодарности

- **Edward Lorenz**, **Otto Rössler**, **Leon Chua** и всем исследователям
  хаоса
- **Иниго Килез** за косинусные палитры
- **Benettin et al.** за QR-метод спектра Ляпунова
- **Grassberger и Procaccia** за корреляционную размерность
- **Richman и Moorman** за Sample Entropy
- **Bandt и Pompe** за Permutation Entropy

## Ссылки

- [GitHub репозиторий](https://github.com/TheGhost1K/ChaosLab)
- [Issues](https://github.com/TheGhost1K/ChaosLab/issues)
- [Discussions](https://github.com/TheGhost1K/ChaosLab/discussions)
