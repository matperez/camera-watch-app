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
            var fileWatcher = new FileWatcherService(config.LogFile1Path, config.LogFile2Path, parser);
            
            // Создаем ViewModel
            var viewModel = new MainWindowViewModel(fileWatcher, parser, config.DisplayDurationSeconds);
            
            // Запускаем мониторинг файлов
            fileWatcher.Start();
            
            // Создаем окно
            desktop.MainWindow = new MainWindow
            {
                DataContext = viewModel,
                Position = new Avalonia.PixelPoint(0, 0)
            };
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
