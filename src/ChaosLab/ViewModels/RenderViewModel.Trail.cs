using ChaosLab.Analysis;
using ChaosLab.Core;

namespace ChaosLab.ViewModels;

public sealed partial class RenderViewModel
{
    // ================================================================
    // TRAIL: СОСТОЯНИЕ
    // ================================================================

    /// <summary>
    /// Живой след траектории с затухающим хвостом.
    /// Хранит последние <see cref="AnimatedTrail.TrailLength"/> точек.
    /// </summary>
    private readonly AnimatedTrail _trail = new();

    /// <summary>
    /// Текущее состояние траектории. Живёт между кадрами, чтобы след
    /// продолжался, а не начинался заново.
    /// </summary>
    private double[]? _trailState;

    /// <summary>Вспомогательный буфер для <see cref="Integrator.StepOnce"/>.</summary>
    private double[]? _trailNext;

    /// <summary>
    /// RK4-буферы для потоков. <c>null</c> для дискретных отображений.
    /// </summary>
    private Rk4Scratch? _trailRk;

    // ================================================================
    // TRAIL: ПУБЛИЧНЫЙ API
    // ================================================================

    /// <summary>Длина следа в точках (для UI и тестов).</summary>
    public int TrailLength
    {
        get => _trail.TrailLength;
        set => _trail.TrailLength = value;
    }

    // ================================================================
    // TRAIL: СБРОС
    // ================================================================

    /// <summary>
    /// Полностью сбрасывает состояние Trail: очищает след и освобождает
    /// буферы. Вызывается при смене режима, системы или старте анимации.
    /// </summary>
    private void ResetTrailState()
    {
        _trail.Clear();
        _trailState = null;
        _trailNext = null;
        _trailRk = null;
    }

    // ================================================================
    // TRAIL: ГЕНЕРАЦИЯ КАДРА
    // ================================================================

    /// <summary>
    /// Вычисляет один кадр в режиме Trail: добавляет 50 новых точек
    /// к следу, рендерит его в буфер пикселей.
    /// </summary>
    /// <param name="cs">Текущая система.</param>
    /// <param name="w">Ширина холста.</param>
    /// <param name="h">Высота холста.</param>
    /// <param name="ct">Токен отмены.</param>
    /// <returns>Массив пикселей и число точек в следе.</returns>
    private (int[], long) ComputeTrailFrame(
        ChaoticSystem cs, int w, int h, CancellationToken ct)
    {
        var p = _system.CurrentParamValues();

        // ─── Проверяем инварианты состояния буферов ───
        // Буферы должны соответствовать текущей системе:
        //   1) быть инициализированы,
        //   2) иметь длину Dimension,
        //   3) для потока — иметь Rk4Scratch,
        //   4) для отображения — НЕ иметь Rk4Scratch.
        bool needsReset =
            _trailState is null
            || _trailNext is null
            || _trailState.Length != cs.Dimension
            || _trailNext.Length != cs.Dimension
            || (!cs.IsDiscrete && _trailRk is null)
            || (cs.IsDiscrete && _trailRk is not null);

        if (needsReset)
        {
            _trail.Clear();
            _trailState = (double[]) cs.DefaultInitialStateCached.Clone();
            _trailNext = new double[cs.Dimension];
            _trailRk = cs.IsDiscrete ? null : new Rk4Scratch(cs.Dimension);

            // Прогрев один раз — выйти на аттрактор
            for (int i = 0; i < 500; i++)
            {
                if ((i & 0x7F) == 0 && ct.IsCancellationRequested)
                    return (_renderer.Pixels, 0);
                Integrator.StepOnce(cs, _trailState, _trailNext, p, Dt, _trailRk);
            }
        }

        // ─── Добавляем 50 точек за кадр ───
        const int batch = 50;
        for (int i = 0; i < batch; i++)
        {
            if ((i & 0xF) == 0 && ct.IsCancellationRequested)
                return (_renderer.Pixels, 0);

            Integrator.StepOnce(cs, _trailState!, _trailNext!, p, Dt, _trailRk);
            cs.Project3D(_trailState!, _xyzBuffer);
            _trail.Add(_xyzBuffer[0], _xyzBuffer[1], _xyzBuffer[2]);
        }

        // ─── Рендер ───
        var trailCam = _camera.Snapshot(w, h);
        Array.Clear(_renderer.Pixels);
        _trail.Render(_renderer.Pixels, w, h, trailCam, 0x57A9FF);

        return (_renderer.Pixels, _trail.TrailLength);
    }
}
