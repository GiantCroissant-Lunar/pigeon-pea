using System.Collections.Generic;

namespace PigeonPea.Game.Contracts.Avatar.Models;

public class AvatarView
{
    public AppearanceData Appearance { get; set; } = new();
    public Dictionary<string, string> CosmeticEquipment { get; set; } = new();
    public string DisplayName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
}
