namespace PigeonPea.Shared.ECS.Components;

public struct Animation
{
    public string CurrentAnimationId;
    public bool IsLooping;
    public float CurrentTime;
    public bool IsFinished;
    public float SpeedMultiplier;
}
