using System.Collections.Generic;

namespace PigeonPea.Game.Contracts.Avatar.Models;

public class AppearanceData
{
    public string BodyType { get; set; } = string.Empty;
    public Dictionary<string, string> Features { get; set; } = new(); // e.g. "Hair" -> "Style1"
    public Dictionary<string, string> Colors { get; set; } = new(); // e.g. "Skin" -> "#FFCCAA"
}
