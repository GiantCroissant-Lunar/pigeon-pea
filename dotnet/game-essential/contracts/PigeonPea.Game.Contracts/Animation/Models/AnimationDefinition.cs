using System.Collections.Generic;

namespace PigeonPea.Game.Contracts.Animation.Models;

public class AnimationDefinition
{
    public string Id { get; set; } = string.Empty;
    public float Duration { get; set; }
    public int FrameCount { get; set; }
    public Dictionary<int, string> FrameEvents { get; set; } = new();
}
