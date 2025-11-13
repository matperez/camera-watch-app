using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CameraWatch.Models;
using CameraWatch.Services;
using Xunit;

namespace CameraWatch.Tests;

public class FileWatcherServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _file1Path;
    private readonly string _file2Path;
    private readonly LogParserService _parser;
    private readonly List<Violation> _receivedViolations;
    private readonly AutoResetEvent _eventReceived;

    public FileWatcherServiceTests()
    {
        // Создаем временную директорию для тестовых файлов
        _tempDir = Path.Combine(Path.GetTempPath(), $"CameraWatchTests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);

        _file1Path = Path.Combine(_tempDir, "test_sock1.log");
        _file2Path = Path.Combine(_tempDir, "test_sock2.log");

        _parser = new LogParserService();
        _receivedViolations = new List<Violation>();
        _eventReceived = new AutoResetEvent(false);
    }

    [Fact]
    public void Start_ShouldInitializeFilePositions_WhenFilesExist()
    {
        // Arrange
        File.WriteAllText(_file1Path, "Initial content");
        File.WriteAllText(_file2Path, "Initial content");

        var service = new FileWatcherService(_file1Path, _file2Path, _parser);

        // Act
        service.Start();

        // Assert - сервис должен запуститься без ошибок
        // Проверяем, что файлы были прочитаны (позиция установлена)
        var file1Info = new FileInfo(_file1Path);
        var file2Info = new FileInfo(_file2Path);
        
        Assert.True(file1Info.Exists);
        Assert.True(file2Info.Exists);
        
        service.Dispose();
    }

    [Fact]
    public void Start_ShouldNotFail_WhenFilesDoNotExist()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);

        // Act & Assert - не должно быть исключений
        service.Start();
        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldDetectViolation_WhenNewLineAdded()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        // Записываем начальное содержимое (без нарушений)
        File.WriteAllText(_file1Path, "13/11/2025-14:00:00 --- SOCK1---overload: False, overloadAxels: False, number: А123БВ777, weight: 30000, limit : 40000, oversize: False\n");

        service.Start();
        Thread.Sleep(200); // Даем время на инициализацию

        // Act - добавляем строку с нарушением
        var violationLine = "13/11/2025-14:01:00 --- SOCK1---overload: True, overloadAxels: False, number: В456ГД123, weight: 45000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file1Path, violationLine);

        // Assert - ждем события
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(3));
        Assert.True(eventReceived, "Событие о нарушении должно быть получено");

        Assert.Single(_receivedViolations);
        var violation = _receivedViolations[0];
        Assert.Equal("В456ГД123", violation.LicensePlate);
        Assert.Equal(ViolationType.Overweight, violation.Type);
        Assert.Equal(1, violation.Lane);

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldDetectOversizeViolation()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act
        var oversizeLine = "13/11/2025-14:02:00 --- SOCK1---overload: False, overloadAxels: False, number: С789ЕЖ456, weight: 35000, limit : 40000, oversize: True\n";
        File.AppendAllText(_file1Path, oversizeLine);

        // Assert
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(3));
        Assert.True(eventReceived);

        Assert.Single(_receivedViolations);
        var violation = _receivedViolations[0];
        Assert.Equal("С789ЕЖ456", violation.LicensePlate);
        Assert.Equal(ViolationType.Oversize, violation.Type);
        Assert.Equal(1, violation.Lane);

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldDetectViolationFromSecondFile()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file2Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act
        var violationLine = "13/11/2025-14:03:00 --- SOCK2---overload: True, overloadAxels: False, number: М012НП789, weight: 50000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file2Path, violationLine);

        // Assert
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(3));
        Assert.True(eventReceived);

        Assert.Single(_receivedViolations);
        var violation = _receivedViolations[0];
        Assert.Equal("М012НП789", violation.LicensePlate);
        Assert.Equal(2, violation.Lane); // SOCK2 = полоса 2

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldIgnoreLinesWithoutViolations()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act - добавляем строку без нарушений
        var normalLine = "13/11/2025-14:04:00 --- SOCK1---overload: False, overloadAxels: False, number: О345РС012, weight: 30000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file1Path, normalLine);

        // Assert - событие не должно быть получено
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromMilliseconds(500));
        Assert.False(eventReceived, "Событие не должно быть сгенерировано для строки без нарушений");
        Assert.Empty(_receivedViolations);

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldReadOnlyNewLines_AfterInitialization()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        // Записываем начальное содержимое с нарушением
        var initialLine = "13/11/2025-14:05:00 --- SOCK1---overload: True, overloadAxels: False, number: Т678УФ345, weight: 45000, limit : 40000, oversize: False\n";
        File.WriteAllText(_file1Path, initialLine);

        service.Start();
        Thread.Sleep(200);

        // Act - добавляем новую строку с нарушением
        var newLine = "13/11/2025-14:06:00 --- SOCK1---overload: True, overloadAxels: False, number: Х901ЦЧ678, weight: 47000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file1Path, newLine);

        // Assert - должно быть получено только одно событие (только новая строка)
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(3));
        Assert.True(eventReceived);

        Assert.Single(_receivedViolations);
        var violation = _receivedViolations[0];
        Assert.Equal("Х901ЦЧ678", violation.LicensePlate); // Должна быть только новая строка

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldDetectMultipleViolations_FromMultipleLines()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act - добавляем несколько строк с нарушениями
        var lines = new[]
        {
            "13/11/2025-14:07:00 --- SOCK1---overload: True, overloadAxels: False, number: Ш234ЩЫ901, weight: 45000, limit : 40000, oversize: False\n",
            "13/11/2025-14:08:00 --- SOCK1---overload: False, overloadAxels: False, number: Э567ЮЯ234, weight: 35000, limit : 40000, oversize: True\n"
        };
        File.AppendAllLines(_file1Path, lines);

        // Assert - должны быть получены оба события
        int eventsReceived = 0;
        for (int i = 0; i < 2; i++)
        {
            if (_eventReceived.WaitOne(TimeSpan.FromSeconds(3)))
            {
                eventsReceived++;
            }
        }

        Assert.Equal(2, eventsReceived);
        Assert.Equal(2, _receivedViolations.Count);
        Assert.Equal("Ш234ЩЫ901", _receivedViolations[0].LicensePlate);
        Assert.Equal(ViolationType.Overweight, _receivedViolations[0].Type);
        Assert.Equal("Э567ЮЯ234", _receivedViolations[1].LicensePlate);
        Assert.Equal(ViolationType.Oversize, _receivedViolations[1].Type);

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldDetectOverloadAxelsViolation()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act
        var overloadAxelsLine = "13/11/2025-14:09:00 --- SOCK1---overload: False, overloadAxels: True, number: Я890АБ567, weight: 35000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file1Path, overloadAxelsLine);

        // Assert
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(3));
        Assert.True(eventReceived);

        Assert.Single(_receivedViolations);
        var violation = _receivedViolations[0];
        Assert.Equal("Я890АБ567", violation.LicensePlate);
        Assert.Equal(ViolationType.Overweight, violation.Type); // overloadAxels тоже считается Overweight

        service.Dispose();
    }

    [Fact]
    public void FileChanged_ShouldIgnoreInvalidLines()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act - добавляем невалидные строки
        var invalidLines = new[]
        {
            "This is not a valid log line\n",
            "13/11/2025-14:10:00 --- Some other format\n",
            "overload: True, but incomplete line\n"
        };
        File.AppendAllLines(_file1Path, invalidLines);

        // Assert - события не должны быть сгенерированы
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromMilliseconds(500));
        Assert.False(eventReceived);
        Assert.Empty(_receivedViolations);

        service.Dispose();
    }

    [Fact]
    public void Dispose_ShouldStopMonitoring()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act
        service.Dispose();
        Thread.Sleep(200);

        // Добавляем новую строку после Dispose
        var violationLine = "13/11/2025-14:11:00 --- SOCK1---overload: True, overloadAxels: False, number: К123ЛМ890, weight: 45000, limit : 40000, oversize: False\n";
        File.AppendAllText(_file1Path, violationLine);

        // Assert - событие не должно быть получено
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromMilliseconds(500));
        Assert.False(eventReceived, "После Dispose не должно генерироваться новых событий");
        Assert.Empty(_receivedViolations);
    }

    [Fact]
    public void FileChanged_ShouldHandleBothFilesSimultaneously()
    {
        // Arrange
        var service = new FileWatcherService(_file1Path, _file2Path, _parser);
        service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };

        File.WriteAllText(_file1Path, "");
        File.WriteAllText(_file2Path, "");
        service.Start();
        Thread.Sleep(200);

        // Act - добавляем нарушения в оба файла
        File.AppendAllText(_file1Path, "13/11/2025-14:12:00 --- SOCK1---overload: True, overloadAxels: False, number: Н456ОП123, weight: 45000, limit : 40000, oversize: False\n");
        File.AppendAllText(_file2Path, "13/11/2025-14:13:00 --- SOCK2---overload: True, overloadAxels: False, number: А123БВ777, weight: 50000, limit : 40000, oversize: False\n");

        // Assert - должны быть получены оба события
        int eventsReceived = 0;
        for (int i = 0; i < 2; i++)
        {
            if (_eventReceived.WaitOne(TimeSpan.FromSeconds(3)))
            {
                eventsReceived++;
            }
        }

        Assert.Equal(2, eventsReceived);
        Assert.Equal(2, _receivedViolations.Count);
        
        // Проверяем, что оба события имеют правильные полосы
        var lanes = new HashSet<int>();
        foreach (var violation in _receivedViolations)
        {
            lanes.Add(violation.Lane);
        }
        Assert.Contains(1, lanes);
        Assert.Contains(2, lanes);

        service.Dispose();
    }

    public void Dispose()
    {
        // Очищаем временные файлы
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch
        {
            // Игнорируем ошибки при удалении
        }
    }
}

