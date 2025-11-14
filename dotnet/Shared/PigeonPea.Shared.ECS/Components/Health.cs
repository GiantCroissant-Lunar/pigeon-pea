namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Health component for entities (monsters, player).
/// </summary>
public struct Health
{
    public int Current { get; set; }
    public int Maximum { get; set; }

    public Health(int maximum)
    {
        Maximum = maximum;
        Current = maximum;
    }

    public Health(int current, int maximum)
    {
        Current = current;
        Maximum = maximum;
    }

    public readonly bool IsDead => Current <= 0;
    public readonly bool IsFullHealth => Current >= Maximum;
    public readonly float HealthPercent => Maximum > 0 ? (float)Current / Maximum : 0f;

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(Health left, Health right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Health left, Health right)
    {
        return !(left == right);
    }
}
