using System.Collections.Generic;

namespace PigeonPea.Game.Contracts.Character.Models;

public class CharacterClass
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, float> StartingStats { get; set; } = new();
}
