namespace PigeonPea.Game.Contracts.Animation.Models;

public class AnimationView
{
    public string CurrentAnimationId { get; set; } = string.Empty;
    public bool IsLooping { get; set; }
    public float CurrentTime { get; set; }
    public int CurrentFrame { get; set; }
    public bool IsFinished { get; set; }
}
