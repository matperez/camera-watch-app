using System;

namespace CameraWatch.Models;

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Source { get; set; } = string.Empty; // SOCK1 или SOCK2
    public string RawLine { get; set; } = string.Empty;
    public bool IsOverload { get; set; }
    public bool IsOverloadAxels { get; set; }
    public bool IsOversize { get; set; }
    public string LicensePlate { get; set; } = string.Empty;
    public int Weight { get; set; }
    public int Limit { get; set; }
    public bool IsValid { get; set; }
}

