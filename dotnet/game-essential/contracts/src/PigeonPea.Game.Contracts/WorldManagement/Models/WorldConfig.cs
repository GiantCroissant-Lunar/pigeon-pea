namespace PigeonPea.Game.Contracts.WorldManagement.Models;

public class WorldConfig
{
    public string Name { get; set; } = string.Empty;
    public int InitialCapacity { get; set; } = 1000;
    public bool EnableSimulation { get; set; }
}
