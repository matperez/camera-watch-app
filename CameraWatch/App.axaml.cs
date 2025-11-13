using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using CameraWatch.ViewModels;
using CameraWatch.Views;
using CameraWatch.Services;
using CameraWatch.Models;

namespace CameraWatch;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            
            // Загружаем конфигурацию
            var configService = new ConfigService();
            var config = configService.LoadConfig();
            
            // Создаем сервисы
            var parser = new LogParserService();
            
            // Выбираем источник событий в зависимости от конфигурации
            IViolationEventSource eventSource = config.UseFakeEvents
                ? new FakeEventGeneratorService()
                : new FileWatcherService(config.LogFile1Path, config.LogFile2Path, parser);
            
            // Создаем ViewModel (подписка на события происходит в конструкторе)
            var viewModel = new MainWindowViewModel(eventSource, parser, config.DisplayDurationSeconds);
            
            System.Console.WriteLine($"Создан ViewModel. Подписка на события выполнена. UseFakeEvents: {config.UseFakeEvents}");
            
            // Запускаем мониторинг источника событий
            System.Console.WriteLine($"Запуск источника событий. Тип: {eventSource.GetType().Name}, UseFakeEvents: {config.UseFakeEvents}");
            eventSource.Start();
            System.Console.WriteLine("Источник событий запущен");
            
            // Для отладки: проверяем, что генератор действительно запустился
            if (eventSource is FakeEventGeneratorService fakeService)
            {
                System.Console.WriteLine("FakeEventGeneratorService обнаружен и запущен");
            }
            
            // Создаем окно
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
                Position = new Avalonia.PixelPoint(100, 100) // Смещаем для лучшей видимости
            };
            
            System.Console.WriteLine($"MainWindow created. UseFakeEvents: {config.UseFakeEvents}");
            
            // Убеждаемся, что окно показывается
            desktop.MainWindow.Show();
            desktop.MainWindow.Activate();
            
            System.Console.WriteLine($"MainWindow shown: IsVisible={desktop.MainWindow.IsVisible}, WindowState={desktop.MainWindow.WindowState}, Position={desktop.MainWindow.Position}");
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
