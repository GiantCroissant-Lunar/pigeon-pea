using SadRogue.Primitives;

namespace PigeonPea.Game.Input.Events;

/// <summary>
/// Event triggered when player requests movement.
/// </summary>
public sealed class MoveInputEvent
{
    public Point Direction { get; set; }
    public float Timestamp { get; set; }

    public MoveInputEvent(Point direction, float timestamp = 0f)
    {
        Direction = direction;
        Timestamp = timestamp;
    }

    public override string ToString() => $"MoveInputEvent(Direction: {Direction}, Timestamp: {Timestamp})";
}
