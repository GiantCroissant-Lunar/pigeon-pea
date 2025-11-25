namespace PigeonPea.Game.Contracts.Persistence.Models;

public class SaveResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}
