namespace PigeonPea.Game.Input.Events;

/// <summary>
/// Event triggered when player requests attack.
/// </summary>
public sealed class AttackInputEvent
{
    public float Timestamp { get; set; }

    public AttackInputEvent(float timestamp = 0f)
    {
        Timestamp = timestamp;
    }

    public override string ToString() => $"AttackInputEvent(Timestamp: {Timestamp})";
}
