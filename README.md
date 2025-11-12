# Camera Watch Display App

Кросс-платформенное приложение на .NET для отображения предупреждений о нарушениях на дороге в режиме реального времени. Приложение мониторит лог-файлы камер контроля движения и отображает информацию о нарушениях (превышение допустимой массы или габаритов) на прозрачном экране, который транслируется водителям.

## Описание

Приложение предназначено для работы с системой контроля движения по автомобильной дороге. Оно отслеживает два лог-файла (по одному для каждой полосы движения) и при обнаружении нарушений отображает предупреждения на экране.

### Основные возможности

- Мониторинг двух лог-файлов в реальном времени
- Автоматическое обнаружение нарушений (превышение массы или габаритов)
- Отображение предупреждений на прозрачном окне
- Разделение экрана на две половины (для двух полос движения)
- Настраиваемая длительность показа предупреждений
- Конфигурация через JSON-файл
- Кросс-платформенность (Windows, macOS, Linux)

## Требования

- .NET 9.0 SDK или выше
- Windows, macOS или Linux

## Установка и сборка

### Предварительные требования

1. Установите .NET 9.0 SDK с [официального сайта](https://dotnet.microsoft.com/download)
2. Установите шаблоны Avalonia (опционально, для создания новых проектов):
```bash
dotnet new install Avalonia.Templates
```

### Сборка проекта

1. Клонируйте репозиторий:
```bash
git clone <repository-url>
cd camera-watch
```

2. Восстановите зависимости:
```bash
dotnet restore
```

3. Соберите проект:
```bash
dotnet build --project CameraWatch/CameraWatch.csproj
```

4. Запустите приложение:
```bash
dotnet run --project CameraWatch/CameraWatch.csproj
```

### Сборка релизной версии

```bash
dotnet build -c Release --project CameraWatch/CameraWatch.csproj
```

### Создание исполняемого файла

#### Windows
```bash
dotnet publish -c Release -r win-x64 --self-contained -o publish/win-x64
```

#### macOS
```bash
dotnet publish -c Release -r osx-x64 --self-contained -o publish/osx-x64
```

#### Linux
```bash
dotnet publish -c Release -r linux-x64 --self-contained -o publish/linux-x64
```

Исполняемый файл будет находиться в папке `publish/<platform>/CameraWatch`.

## Настройка

Настройки приложения хранятся в файле `appsettings.json` в папке `CameraWatch/`. При первом запуске файл будет создан автоматически с настройками по умолчанию.

### Параметры конфигурации

- `LogFile1Path` - путь к первому лог-файлу (SOCK1, левая полоса)
- `LogFile2Path` - путь ко второму лог-файлу (SOCK2, правая полоса)
- `DisplayDurationSeconds` - длительность показа предупреждения в секундах (по умолчанию: 10)
- `DisplayAreaWidth` - ширина области трансляции в пикселях (по умолчанию: 576)
- `DisplayAreaHeight` - высота области трансляции в пикселях (по умолчанию: 192)

### Пример конфигурации

```json
{
  "LogFile1Path": ".examples/camealogSOCK1.log",
  "LogFile2Path": ".examples/camealogSOCK2.log",
  "DisplayDurationSeconds": 10,
  "DisplayAreaWidth": 576,
  "DisplayAreaHeight": 192
}
```

**Примечание:** Можно использовать как абсолютные, так и относительные пути. Относительные пути разрешаются относительно расположения файла `appsettings.json`.

## Формат лог-файлов

Приложение ожидает строки в следующем формате:

```
DD/MM/YYYY-HH:mm:ss --- SOCK1---overload: True, overloadAxels: False, number: А123БВ777, weight: 45000, limit : 40000, oversize: False
```

### Типы нарушений

- **Превышение допустимой массы**: когда `overload: True` или `overloadAxels: True`
- **Превышение допустимых габаритов**: когда `oversize: True`

## Использование

1. Убедитесь, что пути к лог-файлам в `appsettings.json` указаны правильно
2. Запустите приложение
3. Приложение создаст окно в верхнем левом углу экрана (размер 576x192 пикселей)
4. При обнаружении нарушения в лог-файле на соответствующей половине экрана появится предупреждение:
   - "Нарушение!"
   - Гос номер транспортного средства
   - Вид нарушения (превышение допустимой массы / превышение допустимых габаритов)
5. Предупреждение автоматически исчезнет через заданное в конфигурации время и вернется к статичному тексту

## Структура проекта

```
camera-watch/
├── CameraWatch/
│   ├── Models/              # Модели данных (AppConfig, LogEntry, Violation)
│   ├── Services/            # Сервисы (ConfigService, LogParserService, FileWatcherService)
│   ├── ViewModels/          # ViewModels для MVVM (MainWindowViewModel, ViewModelBase)
│   ├── Views/               # Представления (MainWindow.axaml)
│   ├── App.axaml            # Определение приложения
│   ├── Program.cs           # Точка входа
│   ├── ViewLocator.cs       # Локатор представлений
│   ├── CameraWatch.csproj   # Файл проекта
│   └── appsettings.json     # Конфигурация
├── .examples/               # Примеры лог-файлов (в .gitignore)
├── CameraWatch.sln          # Solution файл
└── README.md
```

## CI/CD

Проект использует GitHub Actions для автоматической сборки на Windows и macOS.

### Автоматическая сборка

При каждом push в ветки `main` или `develop`, а также при создании Pull Request, автоматически запускается сборка проекта на следующих платформах:

- **Windows x64** - создается ZIP-архив
- **macOS x64** - создается TAR.GZ-архив
- **macOS ARM64** - создается TAR.GZ-архив

Собранные артефакты доступны в разделе Actions на GitHub и могут быть скачаны для тестирования или распространения.

### Ручной запуск сборки

Вы можете вручную запустить сборку через GitHub Actions:
1. Перейдите в раздел **Actions** репозитория
2. Выберите workflow **Build and Publish**
3. Нажмите **Run workflow**

## Разработка

### Архитектура

Приложение использует паттерн MVVM (Model-View-ViewModel) с использованием:
- **Avalonia UI** - кросс-платформенный UI фреймворк
- **CommunityToolkit.Mvvm** - для реализации ViewModel с атрибутами
- **System.Text.Json** - для работы с конфигурацией
- **FileSystemWatcher** - для мониторинга файлов

### Запуск в режиме разработки

```bash
dotnet run --project CameraWatch/CameraWatch.csproj
```

### Очистка проекта

```bash
dotnet clean --project CameraWatch/CameraWatch.csproj
```

## Технологии

- .NET 9.0
- Avalonia UI 11.3.8 (кросс-платформенный UI фреймворк)
- CommunityToolkit.Mvvm 8.2.1 (MVVM паттерн)
- System.Text.Json 9.0.0 (работа с конфигурацией)
- FileSystemWatcher (мониторинг файлов)

## Лицензия

[Укажите лицензию]

## Автор

[Укажите автора]
