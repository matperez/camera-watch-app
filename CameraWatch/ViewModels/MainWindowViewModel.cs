using System;
using Avalonia.Threading;
using CameraWatch.Models;
using CameraWatch.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraWatch.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IViolationEventSource _eventSource;
    private readonly LogParserService _parser;
    private readonly DispatcherTimer _hideTimer1;
    private readonly DispatcherTimer _hideTimer2;
    private readonly int _displayDurationSeconds;

    [ObservableProperty]
    private string _lane1Text = "ВНИМАНИЕ!\nВЕСОВОЙ\nКОНТРОЛЬ!";

    [ObservableProperty]
    private string _lane2Text = "ВНИМАНИЕ!\nСОБЛЮДАЙТЕ\nСКОРОСТНОЙ\nРЕЖИМ!";

    [ObservableProperty]
    private bool _lane1Visible = true;

    [ObservableProperty]
    private bool _lane2Visible = true;

    public MainWindowViewModel(IViolationEventSource eventSource, LogParserService parser, int displayDurationSeconds)
    {
        _eventSource = eventSource;
        _parser = parser;
        _displayDurationSeconds = displayDurationSeconds;

        // Настраиваем таймеры для автоматического скрытия
        _hideTimer1 = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(displayDurationSeconds)
        };
        _hideTimer1.Tick += (s, e) => HideLane1();

        _hideTimer2 = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(displayDurationSeconds)
        };
        _hideTimer2.Tick += (s, e) => HideLane2();

        // Подписываемся на события нарушений
        _eventSource.ViolationDetected += OnViolationDetected;
        LogMessage("MainWindowViewModel: Подписка на события ViolationDetected выполнена");
        
        // Проверяем, что подписка работает - вызываем тестовое событие напрямую
        if (_eventSource is FakeEventGeneratorService fakeService)
        {
            LogMessage("MainWindowViewModel: Обнаружен FakeEventGeneratorService, проверяем подписку...");
        }
    }

    private void OnViolationDetected(Violation violation)
    {
        LogMessage($"MainWindowViewModel: Получено нарушение - Полоса {violation.Lane}, Номер: {violation.LicensePlate}, Тип: {violation.Type}");
        
        // Убеждаемся, что обновления UI происходят в UI потоке
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var violationText = _parser.GetViolationTypeText(violation.Type);
                var displayText = $"Нарушение!\n{violation.LicensePlate}\n{violationText}";
                
                LogMessage($"MainWindowViewModel: Обновление UI - Полоса {violation.Lane}, Текст: {displayText}");

                if (violation.Lane == 1)
                {
                    Lane1Text = displayText;
                    Lane1Visible = true;
                    _hideTimer1.Stop();
                    _hideTimer1.Start();
                    LogMessage("MainWindowViewModel: Обновлена полоса 1");
                }
                else if (violation.Lane == 2)
                {
                    Lane2Text = displayText;
                    Lane2Visible = true;
                    _hideTimer2.Stop();
                    _hideTimer2.Start();
                    LogMessage("MainWindowViewModel: Обновлена полоса 2");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"MainWindowViewModel: Ошибка обновления UI: {ex.Message}");
            }
        });
    }
    
    private void LogMessage(string message)
    {
        try
        {
            System.Console.WriteLine(message);
            System.Diagnostics.Debug.WriteLine(message);
            var logPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "camera-watch-debug.log");
            System.IO.File.AppendAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
        }
        catch { }
    }

    private void HideLane1()
    {
        // Возвращаем статичную надпись
        Lane1Text = "ВНИМАНИЕ!\nВЕСОВОЙ\nКОНТРОЛЬ!";
        Lane1Visible = true;
        _hideTimer1.Stop();
    }

    private void HideLane2()
    {
        // Возвращаем статичную надпись
        Lane2Text = "ВНИМАНИЕ!\nСОБЛЮДАЙТЕ\nСКОРОСТНОЙ\nРЕЖИМ!";
        Lane2Visible = true;
        _hideTimer2.Stop();
    }
}
