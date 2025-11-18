namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Data component for map markers (points of interest).
/// </summary>
public struct MarkerData
{
    public string MarkerType { get; set; } // "quest", "dungeon", "landmark", etc.
    public string Title { get; set; }
    public bool Discovered { get; set; }

    public MarkerData(string markerType, string title, bool discovered = false)
    {
        MarkerType = markerType;
        Title = title;
        Discovered = discovered;
    }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(MarkerData left, MarkerData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MarkerData left, MarkerData right)
    {
        return !(left == right);
    }
}
