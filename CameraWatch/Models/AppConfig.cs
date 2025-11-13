namespace CameraWatch.Models;

public class AppConfig
{
    public string LogFile1Path { get; set; } = string.Empty;
    public string LogFile2Path { get; set; } = string.Empty;
    public int DisplayDurationSeconds { get; set; } = 10;
    public int DisplayAreaWidth { get; set; } = 576;
    public int DisplayAreaHeight { get; set; } = 192;
    public bool UseFakeEvents { get; set; } = false;
}

