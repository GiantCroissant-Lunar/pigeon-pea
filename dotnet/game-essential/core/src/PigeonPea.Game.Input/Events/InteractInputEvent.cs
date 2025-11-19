namespace PigeonPea.Game.Input.Events;

/// <summary>
/// Event triggered when player requests interaction.
/// </summary>
public sealed class InteractInputEvent
{
    public float Timestamp { get; set; }

    public InteractInputEvent(float timestamp = 0f)
    {
        Timestamp = timestamp;
    }

    public override string ToString() => $"InteractInputEvent(Timestamp: {Timestamp})";
}
