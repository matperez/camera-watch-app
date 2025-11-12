using System.IO;
using CameraWatch.Models;

namespace CameraWatch.Services
{
    public class FileWatcherService : IDisposable
    {
        private readonly string _filePath1;
        private readonly string _filePath2;
        private readonly LogParserService _parser;
        private FileSystemWatcher? _watcher1;
        private FileSystemWatcher? _watcher2;
        private long _lastPosition1;
        private long _lastPosition2;
        private bool _disposed = false;

        public event Action<Violation>? ViolationDetected;

        public FileWatcherService(string filePath1, string filePath2, LogParserService parser)
        {
            _filePath1 = filePath1;
            _filePath2 = filePath2;
            _parser = parser;
        }

        public void Start()
        {
            // Инициализируем позиции чтения
            InitializeFilePositions();

            // Настраиваем FileSystemWatcher для первого файла
            if (File.Exists(_filePath1))
            {
                var directory1 = Path.GetDirectoryName(_filePath1) ?? string.Empty;
                var fileName1 = Path.GetFileName(_filePath1);

                _watcher1 = new FileSystemWatcher(directory1, fileName1)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _watcher1.Changed += (sender, e) => OnFileChanged(_filePath1, "SOCK1", ref _lastPosition1);
            }

            // Настраиваем FileSystemWatcher для второго файла
            if (File.Exists(_filePath2))
            {
                var directory2 = Path.GetDirectoryName(_filePath2) ?? string.Empty;
                var fileName2 = Path.GetFileName(_filePath2);

                _watcher2 = new FileSystemWatcher(directory2, fileName2)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                    EnableRaisingEvents = true
                };
                _watcher2.Changed += (sender, e) => OnFileChanged(_filePath2, "SOCK2", ref _lastPosition2);
            }

            // Читаем существующие строки при старте (если нужно)
            // ProcessExistingLines();
        }

        private void InitializeFilePositions()
        {
            if (File.Exists(_filePath1))
            {
                _lastPosition1 = new FileInfo(_filePath1).Length;
            }

            if (File.Exists(_filePath2))
            {
                _lastPosition2 = new FileInfo(_filePath2).Length;
            }
        }

        private void OnFileChanged(string filePath, string source, ref long lastPosition)
        {
            try
            {
                // Небольшая задержка, чтобы файл был полностью записан
                Thread.Sleep(100);

                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                fileStream.Seek(lastPosition, SeekOrigin.Begin);

                using var reader = new StreamReader(fileStream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var entry = _parser.ParseLine(line, source);
                    if (entry != null)
                    {
                        var violation = _parser.ExtractViolation(entry);
                        if (violation != null)
                        {
                            ViolationDetected?.Invoke(violation);
                        }
                    }
                }

                lastPosition = fileStream.Position;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка чтения файла {filePath}: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _watcher1?.Dispose();
            _watcher2?.Dispose();
            _disposed = true;
        }
    }
}

