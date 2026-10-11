namespace ChaosLab.Services;

/// <summary>
/// Асинхронный планировщик рендера с коалесцированием запросов.
/// </summary>
/// <remarks>
/// <para>Решает проблему «пользователь тащит слайдер → 60 запросов/sec»:
/// если во время рендера поступает новый запрос, текущий кадр помечается
/// устаревшим и отбрасывается. Выполняется только <b>последний</b> запрос.</para>
/// <para><b>Не бросает</b> исключений наружу. <see cref="OperationCanceledException"/>
/// молча проглатывается — это нормальное завершение.</para>
/// <para>CTS принадлежит планировщику: каждый <see cref="Request"/> создаёт
/// свежий токен, отменяя предыдущий.</para>
/// </remarks>
public sealed class RenderScheduler
{
    private readonly Lock _lock = new();
    private bool _running;
    private bool _pending;
    private CancellationTokenSource? _cts;

    /// <summary>Событие успешного кадра: (pixels, totalPoints).</summary>
    public event Action<int[], long>? FrameReady;

    /// <summary>Событие ошибки: текст исключения (не вызывается при отмене).</summary>
    public event Action<string>? Failed;

    /// <summary>
    /// Фабрика кадра. Получает токен отмены; сама не должна его хранить.
    /// Возвращает (pixels, totalPoints).
    /// </summary>
    public Func<CancellationToken, (int[] pixels, long points)>? FrameFactory { get; set; }

    /// <summary>Запрашивает новый кадр. Отменяет предыдущий незавершённый.</summary>
    public void Request()
    {
        bool start;
        lock (_lock)
        {
            _pending = true;

            // Отменяем предыдущую работу (если была).
            try { _cts?.Cancel(); } catch { }
            try { _cts?.Dispose(); } catch { }
            _cts = new CancellationTokenSource();

            start = !_running;
            if (start) _running = true;
        }

        if (start) _ = RunAsync();
    }

    /// <summary>Отменяет текущий рендер.</summary>
    public void Cancel()
    {
        lock (_lock) { try { _cts?.Cancel(); } catch { } }
    }

    private async Task RunAsync()
    {
        while (true)
        {
            CancellationToken token;
            lock (_lock)
            {
                if (!_pending) { _running = false; return; }
                _pending = false;
                token = _cts!.Token;
            }

            (int[] pixels, long pts) result;
            try
            {
                var f = FrameFactory;
                if (f == null) continue;
                // НЕ передаём токен в Task.Run — иначе .NET сам бросит TaskCanceledException,
                // даже если делегат завершился нормально. Проверка отмены — внутри f.
                result = await Task.Run(() => f(token)).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Отмена — штатная ситуация. Идём на следующую итерацию,
                // где либо увидим _pending = true (свежий запрос), либо выйдем.
                continue;
            }
            catch (Exception ex)
            {
                try { Failed?.Invoke(ex.Message); } catch { }
                continue;
            }

            // Пока считали — пришёл новый запрос. Этот кадр устарел — выбрасываем.
            lock (_lock) { if (_pending) continue; }

            try { FrameReady?.Invoke(result.pixels, result.pts); }
            catch { /* устаревший размер битмапа — просто пропускаем кадр */ }
        }
    }

    /// <summary>Показывает, выполняется ли рендер прямо сейчас.</summary>
    public bool IsBusy { get { lock (_lock) return _running; } }
}
