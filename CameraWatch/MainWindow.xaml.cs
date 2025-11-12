using System.Windows;
using System.Windows.Media;
using CameraWatch.Models;
using CameraWatch.Services;
using CameraWatch.ViewModels;

namespace CameraWatch
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private readonly FileWatcherService _fileWatcher;

        public MainWindow()
        {
            InitializeComponent();

            // Загружаем конфигурацию
            var configService = new ConfigService();
            var config = configService.LoadConfig();

            // Устанавливаем размер окна из конфига
            Width = config.DisplayAreaWidth;
            Height = config.DisplayAreaHeight;

            // Позиционируем окно в верхнем левом углу
            Left = 0;
            Top = 0;

            // Создаем сервисы
            var parser = new LogParserService();
            _fileWatcher = new FileWatcherService(config.LogFile1Path, config.LogFile2Path, parser);

            // Создаем ViewModel
            _viewModel = new MainViewModel(_fileWatcher, parser, config.DisplayDurationSeconds);
            DataContext = _viewModel;

            // Запускаем мониторинг файлов
            _fileWatcher.Start();
        }

        protected override void OnClosed(EventArgs e)
        {
            _fileWatcher?.Dispose();
            base.OnClosed(e);
        }
    }
}

