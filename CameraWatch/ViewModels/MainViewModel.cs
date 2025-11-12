using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using CameraWatch.Models;
using CameraWatch.Services;

namespace CameraWatch.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly FileWatcherService _fileWatcher;
        private readonly LogParserService _parser;
        private readonly DispatcherTimer _hideTimer1;
        private readonly DispatcherTimer _hideTimer2;
        private readonly int _displayDurationSeconds;

        private string _lane1Text = string.Empty;
        private string _lane2Text = string.Empty;
        private bool _lane1Visible = false;
        private bool _lane2Visible = false;

        public MainViewModel(FileWatcherService fileWatcher, LogParserService parser, int displayDurationSeconds)
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

        public string Lane1Text
        {
            get => _lane1Text;
            set
            {
                _lane1Text = value;
                OnPropertyChanged();
            }
        }

        public string Lane2Text
        {
            get => _lane2Text;
            set
            {
                _lane2Text = value;
                OnPropertyChanged();
            }
        }

        public bool Lane1Visible
        {
            get => _lane1Visible;
            set
            {
                _lane1Visible = value;
                OnPropertyChanged();
            }
        }

        public bool Lane2Visible
        {
            get => _lane2Visible;
            set
            {
                _lane2Visible = value;
                OnPropertyChanged();
            }
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
            Lane1Visible = false;
            _hideTimer1.Stop();
        }

        private void HideLane2()
        {
            Lane2Visible = false;
            _hideTimer2.Stop();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

