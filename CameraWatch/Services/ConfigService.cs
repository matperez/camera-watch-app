using System;
using System.IO;
using System.Text.Json;
using CameraWatch.Models;

namespace CameraWatch.Services;

public class ConfigService
{
    private const string ConfigFileName = "appsettings.json";
    private readonly string _configPath;

    public ConfigService()
    {
        var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        _configPath = Path.Combine(appDirectory, ConfigFileName);
    }

    public AppConfig LoadConfig()
    {
        if (!File.Exists(_configPath))
        {
            // Создаем конфигурацию по умолчанию
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var examplesPath = Path.Combine(baseDir, "..", "..", "..", ".examples");
            var defaultConfig = new AppConfig
            {
                LogFile1Path = Path.GetFullPath(Path.Combine(examplesPath, "camealogSOCK1.log")),
                LogFile2Path = Path.GetFullPath(Path.Combine(examplesPath, "camealogSOCK2.log")),
                DisplayDurationSeconds = 10,
                DisplayAreaWidth = 576,
                DisplayAreaHeight = 192
            };
            SaveConfig(defaultConfig);
            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(_configPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config != null)
            {
                // Преобразуем относительные пути в абсолютные
                var configDir = Path.GetDirectoryName(_configPath) ?? AppDomain.CurrentDomain.BaseDirectory;
                if (!Path.IsPathRooted(config.LogFile1Path))
                {
                    config.LogFile1Path = Path.GetFullPath(Path.Combine(configDir, config.LogFile1Path));
                }
                if (!Path.IsPathRooted(config.LogFile2Path))
                {
                    config.LogFile2Path = Path.GetFullPath(Path.Combine(configDir, config.LogFile2Path));
                }
            }

            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            // В случае ошибки возвращаем конфигурацию по умолчанию
            System.Diagnostics.Debug.WriteLine($"Ошибка загрузки конфигурации: {ex.Message}");
            return new AppConfig();
        }
    }

    public void SaveConfig(AppConfig config)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(_configPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка сохранения конфигурации: {ex.Message}");
        }
    }
}

