namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Name component for entities.
/// </summary>
public readonly record struct Name(string Value)
{
    public static implicit operator string(Name name) => name.Value;
    public static implicit operator Name(string value) => new(value);

    public Name ToName()
    {
        throw new NotImplementedException();
    }
}
