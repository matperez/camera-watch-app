using System;
using Avalonia.Threading;
using CameraWatch.Models;
using CameraWatch.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraWatch.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly FileWatcherService _fileWatcher;
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

    public MainWindowViewModel(FileWatcherService fileWatcher, LogParserService parser, int displayDurationSeconds)
    {
        _fileWatcher = fileWatcher;
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
        _fileWatcher.ViolationDetected += OnViolationDetected;
    }

    private void OnViolationDetected(Violation violation)
    {
        var violationText = _parser.GetViolationTypeText(violation.Type);
        var displayText = $"Нарушение!\n{violation.LicensePlate}\n{violationText}";

        if (violation.Lane == 1)
        {
            Lane1Text = displayText;
            Lane1Visible = true;
            _hideTimer1.Stop();
            _hideTimer1.Start();
        }
        else if (violation.Lane == 2)
        {
            Lane2Text = displayText;
            Lane2Visible = true;
            _hideTimer2.Stop();
            _hideTimer2.Start();
        }
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
