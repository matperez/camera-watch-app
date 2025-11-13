using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CameraWatch.Models;
using CameraWatch.Services;
using Xunit;

namespace CameraWatch.Tests;

public class FakeEventGeneratorServiceTests : IDisposable
{
    private readonly FakeEventGeneratorService _service;
    private readonly List<Violation> _receivedViolations;
    private readonly AutoResetEvent _eventReceived;

    public FakeEventGeneratorServiceTests()
    {
        _service = new FakeEventGeneratorService();
        _receivedViolations = new List<Violation>();
        _eventReceived = new AutoResetEvent(false);

        _service.ViolationDetected += (violation) =>
        {
            _receivedViolations.Add(violation);
            _eventReceived.Set();
        };
    }

    [Fact]
    public void Start_ShouldBeginGeneratingEvents()
    {
        // Arrange & Act
        _service.Start();

        // Assert
        // Ждем получения первого события (максимум 2 секунды)
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(2));

        Assert.True(eventReceived, "Генератор должен сгенерировать событие в течение 2 секунд");
        Assert.Single(_receivedViolations);
    }

    [Fact]
    public void GeneratedViolation_ShouldHaveValidProperties()
    {
        // Arrange & Act
        _service.Start();
        bool eventReceived = _eventReceived.WaitOne(TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(eventReceived, "Событие должно быть получено");
        var violation = _receivedViolations[0];

        // Проверяем, что все свойства заполнены
        Assert.NotNull(violation);
        Assert.NotEmpty(violation.LicensePlate);
        Assert.True(violation.Lane == 1 || violation.Lane == 2, "Полоса должна быть 1 или 2");
        Assert.True(violation.Type == ViolationType.Overweight || violation.Type == ViolationType.Oversize, 
            "Тип нарушения должен быть Overweight или Oversize");
        Assert.True(violation.Timestamp <= DateTime.Now, "Время должно быть в прошлом или настоящем");
    }

    [Fact]
    public void GeneratedViolations_ShouldHaveDifferentValues()
    {
        // Arrange
        _service.Start();
        var violations = new List<Violation>();

        // Act - собираем несколько событий
        for (int i = 0; i < 5; i++)
        {
            if (_eventReceived.WaitOne(TimeSpan.FromSeconds(6)))
            {
                violations.Add(_receivedViolations[_receivedViolations.Count - 1]);
            }
        }

        // Assert - проверяем, что есть хотя бы 2 разных события
        Assert.True(violations.Count >= 2, "Должно быть сгенерировано хотя бы 2 события");

        // Проверяем, что есть разнообразие в данных
        var uniquePlates = new HashSet<string>();
        var uniqueLanes = new HashSet<int>();
        var uniqueTypes = new HashSet<ViolationType>();

        foreach (var violation in violations)
        {
            uniquePlates.Add(violation.LicensePlate);
            uniqueLanes.Add(violation.Lane);
            uniqueTypes.Add(violation.Type);
        }

        // Хотя бы одно свойство должно варьироваться
        var hasVariety = uniquePlates.Count > 1 || uniqueLanes.Count > 1 || uniqueTypes.Count > 1;
        Assert.True(hasVariety, "События должны иметь некоторое разнообразие в данных");
    }

    [Fact]
    public void Start_ShouldGenerateEventsPeriodically()
    {
        // Arrange & Act
        _service.Start();

        // Ждем получения нескольких событий
        var events = new List<Violation>();
        for (int i = 0; i < 3; i++)
        {
            if (_eventReceived.WaitOne(TimeSpan.FromSeconds(6)))
            {
                events.Add(_receivedViolations[_receivedViolations.Count - 1]);
            }
        }

        // Assert
        Assert.True(events.Count >= 2, "Должно быть сгенерировано хотя бы 2 события за период времени");
    }

    [Fact]
    public void Dispose_ShouldStopGeneratingEvents()
    {
        // Arrange
        _service.Start();
        
        // Ждем первого события
        _eventReceived.WaitOne(TimeSpan.FromSeconds(2));
        int countBeforeDispose = _receivedViolations.Count;

        // Act
        _service.Dispose();
        Thread.Sleep(3000); // Ждем 3 секунды

        // Assert
        int countAfterDispose = _receivedViolations.Count;
        Assert.Equal(countBeforeDispose, countAfterDispose);
    }

    [Fact]
    public void LicensePlate_ShouldBeFromPredefinedList()
    {
        // Arrange
        var validPlates = new HashSet<string>
        {
            "А123БВ777", "В456ГД123", "С789ЕЖ456", "М012НП789",
            "О345РС012", "Т678УФ345", "Х901ЦЧ678", "Ш234ЩЫ901",
            "Э567ЮЯ234", "Я890АБ567", "К123ЛМ890", "Н456ОП123"
        };

        _service.Start();

        // Act - собираем несколько событий
        var violations = new List<Violation>();
        for (int i = 0; i < 10; i++)
        {
            if (_eventReceived.WaitOne(TimeSpan.FromSeconds(6)))
            {
                violations.Add(_receivedViolations[_receivedViolations.Count - 1]);
            }
        }

        // Assert
        Assert.True(violations.Count > 0, "Должно быть сгенерировано хотя бы одно событие");
        foreach (var violation in violations)
        {
            Assert.True(validPlates.Contains(violation.LicensePlate), 
                $"Госномер '{violation.LicensePlate}' должен быть из предопределенного списка");
        }
    }

    public void Dispose()
    {
        _service?.Dispose();
        _eventReceived?.Dispose();
    }
}

