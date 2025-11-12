using System;
using System.Text.RegularExpressions;
using CameraWatch.Models;

namespace CameraWatch.Services;

public class LogParserService
{
    // Формат: overload: True/False, overloadAxels: True/False, number: XXX, weight: XXX, limit : XXX, oversize: True/False
    private static readonly Regex OverloadPattern = new Regex(
        @"overload:\s*(?<overload>True|False),\s*overloadAxels:\s*(?<overloadAxels>True|False),\s*number:\s*(?<number>[^,]+),\s*weight:\s*(?<weight>\d+),\s*limit\s*:\s*(?<limit>\d+),\s*oversize:\s*(?<oversize>True|False)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public LogEntry? ParseLine(string line, string source)
    {
        if (string.IsNullOrWhiteSpace(line))
            return null;

        // Парсим только строки с форматом overload
        var match = OverloadPattern.Match(line);
        if (!match.Success)
            return null;

        try
        {
            var entry = new LogEntry
            {
                RawLine = line,
                Source = source,
                IsOverload = match.Groups["overload"].Value.Equals("True", StringComparison.OrdinalIgnoreCase),
                IsOverloadAxels = match.Groups["overloadAxels"].Value.Equals("True", StringComparison.OrdinalIgnoreCase),
                IsOversize = match.Groups["oversize"].Value.Equals("True", StringComparison.OrdinalIgnoreCase),
                LicensePlate = match.Groups["number"].Value.Trim(),
                Weight = int.TryParse(match.Groups["weight"].Value, out var weight) ? weight : 0,
                Limit = int.TryParse(match.Groups["limit"].Value, out var limit) ? limit : 0,
                IsValid = true
            };

            // Парсим дату из начала строки (формат: DD/MM/YYYY-HH:mm:ss)
            var dateMatch = Regex.Match(line, @"(\d{2}/\d{2}/\d{4}-\d{2}:\d{2}:\d{2})");
            if (dateMatch.Success)
            {
                if (DateTime.TryParseExact(dateMatch.Groups[1].Value, "dd/MM/yyyy-HH:mm:ss", null, System.Globalization.DateTimeStyles.None, out var timestamp))
                {
                    entry.Timestamp = timestamp;
                }
                else
                {
                    entry.Timestamp = DateTime.Now;
                }
            }
            else
            {
                entry.Timestamp = DateTime.Now;
            }

            return entry;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Ошибка парсинга строки: {ex.Message}");
            return null;
        }
    }

    public Violation? ExtractViolation(LogEntry entry)
    {
        if (entry == null || !entry.IsValid)
            return null;

        // Проверяем наличие нарушений
        ViolationType violationType = ViolationType.None;

        if (entry.IsOverload || entry.IsOverloadAxels)
        {
            violationType = ViolationType.Overweight;
        }
        else if (entry.IsOversize)
        {
            violationType = ViolationType.Oversize;
        }

        // Если нарушений нет, возвращаем null
        if (violationType == ViolationType.None)
            return null;

        // Определяем полосу по источнику (SOCK1 = полоса 1, SOCK2 = полоса 2)
        int lane = entry.Source.Contains("SOCK1", StringComparison.OrdinalIgnoreCase) ? 1 : 2;

        return new Violation
        {
            LicensePlate = entry.LicensePlate,
            Type = violationType,
            Timestamp = entry.Timestamp,
            Lane = lane
        };
    }

    public string GetViolationTypeText(ViolationType type)
    {
        return type switch
        {
            ViolationType.Overweight => "превышение допустимой массы",
            ViolationType.Oversize => "превышение допустимых габаритов",
            _ => string.Empty
        };
    }
}

