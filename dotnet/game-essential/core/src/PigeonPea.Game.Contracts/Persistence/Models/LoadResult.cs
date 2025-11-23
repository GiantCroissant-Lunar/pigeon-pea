namespace PigeonPea.Game.Contracts.Persistence.Models;

public class LoadResult
{
    public bool Success { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int EntitiesLoaded { get; set; }
}
