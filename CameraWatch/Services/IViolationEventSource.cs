using System;
using CameraWatch.Models;

namespace CameraWatch.Services;

/// <summary>
/// Интерфейс для источника событий о нарушениях.
/// Позволяет абстрагировать реальное чтение из файлов от генерации тестовых событий.
/// </summary>
public interface IViolationEventSource : IDisposable
{
    /// <summary>
    /// Событие, возникающее при обнаружении нарушения.
    /// </summary>
    event Action<Violation>? ViolationDetected;

    /// <summary>
    /// Запускает мониторинг источника событий.
    /// </summary>
    void Start();
}

