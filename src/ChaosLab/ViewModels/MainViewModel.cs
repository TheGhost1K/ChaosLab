using System.Windows;

namespace ChaosLab.ViewModels;

/// <summary>
/// Корневая ViewModel приложения. Содержит все дочерние VM и команды
/// верхнего уровня.
/// </summary>
public sealed class MainViewModel : BaseViewModel
{
    /// <summary>Система и её параметры.</summary>
    public SystemViewModel System { get; }
    /// <summary>Камера.</summary>
    public CameraViewModel Camera { get; }
    /// <summary>Рендер.</summary>
    public RenderViewModel Render { get; }
    /// <summary>Анализ.</summary>
    public AnalysisViewModel Analysis { get; }

    /// <summary>Команда: мгновенный рендер.</summary>
    public RelayCommand RenderCommand { get; }
    /// <summary>Команда: очистить холст.</summary>
    public RelayCommand ClearCommand { get; }
    /// <summary>Команда: сбросить камеру.</summary>
    public RelayCommand ResetCameraCommand { get; }
    /// <summary>Команда: сохранить PNG.</summary>
    public RelayCommand SavePngCommand { get; }
    /// <summary>Команда: отменить все задачи анализа.</summary>
    public RelayCommand CancelAllCommand { get; }

    /// <summary>
    /// Создаёт корневую ViewModel: инициализирует дочерние VM, регистрирует
    /// команды и запускает первый рендер через <c>Dispatcher.BeginInvoke</c>.
    /// </summary>
    public MainViewModel()
    {
        System = new SystemViewModel();
        Camera = new CameraViewModel();
        Render = new RenderViewModel(System, Camera);
        Analysis = new AnalysisViewModel(System, Render);

        RenderCommand = new RelayCommand(() => Render.RequestRender());
        ClearCommand = new RelayCommand(() => Render.Clear());
        ResetCameraCommand = new RelayCommand(() => Render.ResetCameraAndRender());
        SavePngCommand = new RelayCommand(SavePng);
        CancelAllCommand = new RelayCommand(() => Analysis.CancelCommand.Execute(null));

        // Автозапуск первого рендера
        Application.Current?.Dispatcher.BeginInvoke(new Action(() => Render.RequestRender()));
    }

    private void SavePng()
    {
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png",
            FileName = $"chaos_{System.Current.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };
        if (dlg.ShowDialog() != true) return;
        try { Render.SavePng(dlg.FileName); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка сохранения"); }
    }
}
