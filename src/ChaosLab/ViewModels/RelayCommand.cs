using System.Windows.Input;

namespace ChaosLab.ViewModels;

/// <summary>
/// Простая реализация <see cref="ICommand"/> для MVVM.
/// </summary>
/// <remarks>
/// Поддерживает два конструктора: с делегатом без параметра (типичный случай)
/// и с параметром. <c>CanExecuteChanged</c> перенаправлен на
/// <see cref="CommandManager.RequerySuggested"/> — WPF автоматически опрашивает
/// состояние команды при изменениях в UI.
/// </remarks>
public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _can;

    /// <summary>Создаёт команду без параметра.</summary>
    /// <param name="execute">Действие.</param>
    /// <param name="can">Опциональная проверка доступности.</param>
    public RelayCommand(Action execute, Func<bool>? can = null)
        : this(_ => execute(), can == null ? null : _ => can()) { }

    /// <summary>Создаёт команду с параметром.</summary>
    /// <param name="execute">Действие с параметром.</param>
    /// <param name="can">Опциональная проверка доступности.</param>
    public RelayCommand(Action<object?> execute, Func<object?, bool>? can = null)
    { _execute = execute; _can = can; }

    /// <inheritdoc/>
    public bool CanExecute(object? p) => _can?.Invoke(p) ?? true;

    /// <inheritdoc/>
    public void Execute(object? p) => _execute(p);

    /// <inheritdoc/>
    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
