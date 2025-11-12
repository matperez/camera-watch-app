using System;

namespace CameraWatch.Models;

public class Violation
{
    public string LicensePlate { get; set; } = string.Empty;
    public ViolationType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public int Lane { get; set; } // 1 или 2
}

public enum ViolationType
{
    None,
    Overweight,      // Превышение допустимой массы
    Oversize         // Превышение допустимых габаритов
}

