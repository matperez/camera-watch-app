using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CameraWatch.Models;

namespace CameraWatch.Services;

/// <summary>
/// Генератор фейковых событий для тестирования приложения.
/// Генерирует случайные нарушения каждые 1-5 секунд.
/// </summary>
public class FakeEventGeneratorService : IViolationEventSource
{
    private readonly Random _random = new Random();
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _generatorTask;
    private bool _disposed = false;

    // Примеры госномеров для генерации
    private readonly string[] _licensePlates = new[]
    {
        "А123БВ777", "В456ГД123", "С789ЕЖ456", "М012НП789",
        "О345РС012", "Т678УФ345", "Х901ЦЧ678", "Ш234ЩЫ901",
        "Э567ЮЯ234", "Я890АБ567", "К123ЛМ890", "Н456ОП123"
    };

    public event Action<Violation>? ViolationDetected;

    public void Start()
    {
        if (_generatorTask != null && !_generatorTask.IsCompleted)
            return;

        LogMessage("FakeEventGeneratorService: Запуск генератора событий");
        _cancellationTokenSource = new CancellationTokenSource();
        _generatorTask = Task.Run(() => GenerateEventsAsync(_cancellationTokenSource.Token));
        
        // Генерируем первое событие сразу для теста
        Task.Run(async () =>
        {
            await Task.Delay(1000); // Задержка для инициализации UI
            var violation = GenerateRandomViolation();
            LogMessage($"FakeEventGeneratorService: Генерация первого события - Полоса {violation.Lane}, Номер: {violation.LicensePlate}, Тип: {violation.Type}");
            ViolationDetected?.Invoke(violation);
            LogMessage("FakeEventGeneratorService: Событие отправлено");
        });
    }

    private async Task GenerateEventsAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Генерируем случайную задержку от 1 до 5 секунд
                var delay = _random.Next(1000, 5001);
                await Task.Delay(delay, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                    break;

                // Генерируем случайное нарушение
                var violation = GenerateRandomViolation();
                LogMessage($"Генерация события: Полоса {violation.Lane}, Номер: {violation.LicensePlate}, Тип: {violation.Type}");
                ViolationDetected?.Invoke(violation);
                LogMessage("FakeEventGeneratorService: Событие отправлено");
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка генерации события: {ex.Message}");
            }
        }
    }

    private Violation GenerateRandomViolation()
    {
        // Случайная полоса (1 или 2)
        var lane = _random.Next(1, 3);

        // Случайный тип нарушения
        var violationType = (ViolationType)_random.Next(1, 3); // Overweight или Oversize

        // Случайный госномер
        var licensePlate = _licensePlates[_random.Next(_licensePlates.Length)];

        return new Violation
        {
            LicensePlate = licensePlate,
            Type = violationType,
            Timestamp = DateTime.Now,
            Lane = lane
        };
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _cancellationTokenSource?.Cancel();
        
        try
        {
            _generatorTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch (AggregateException)
        {
            // Игнорируем ошибки при ожидании завершения задачи
        }

        _cancellationTokenSource?.Dispose();
        _disposed = true;
    }

    private void LogMessage(string message)
    {
        try
        {
            System.Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
            // Также пишем в файл для диагностики
            var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "camera-watch-debug.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch
        {
            // Игнорируем ошибки логирования
        }
    }
}

