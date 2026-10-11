using System.Windows;

namespace ChaosLab;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Точка входа приложения. Настраивает обработчики исключений:
    /// молча игнорирует <see cref="OperationCanceledException"/>
    /// (штатная отмена фоновых задач) и показывает диалог для остальных.
    /// </summary>
    /// <param name="e">Аргументы запуска (передаются базовому классу).</param>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Замораживаем все Freezable-ресурсы (кисти, трансформации). После
        // Freeze() WPF перестаёт наблюдать за ними — минус несколько процентов
        // на отрисовке UI. Все ресурсы статические, поэтому безопасно.

        // Снимаем ключи в отдельный список, потому что нельзя модифицировать
        // коллекцию во время foreach (Resources[key] может её изменить).
        var keys = new List<object>(Resources.Keys.Count);
        foreach (object k in Resources.Keys) keys.Add(k);

        for (int i = 0; i < keys.Count; i++)
        {
            if (Resources[keys[i]] is Freezable f && f.CanFreeze)
                f.Freeze();
        }

        // Отменённые фоновые задачи — это НЕ ошибка приложения.
        TaskScheduler.UnobservedTaskException += (_, args) => args.SetObserved();

        DispatcherUnhandledException += (_, args) =>
        {
            // Отмена рендера/анализа — штатное завершение, не показываем диалог.
            if (args.Exception is OperationCanceledException)
            {
                args.Handled = true;
                return;
            }

            MessageBox.Show(args.Exception.ToString(), "ChaosLab — ошибка",
                MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
    }
}
