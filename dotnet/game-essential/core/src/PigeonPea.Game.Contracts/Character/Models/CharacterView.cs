namespace PigeonPea.Game.Contracts.Character.Models;

public class CharacterView
{
    public string Name { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public int Level { get; set; }
    public int Experience { get; set; }
    public int ExperienceToNextLevel { get; set; }
}
