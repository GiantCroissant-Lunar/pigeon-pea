namespace PigeonPea.Shared.ECS.Components;

/// <summary>
/// Data component for city entities on the world map.
/// </summary>
public struct CityData
{
    public string CityName { get; set; }
    public int Population { get; set; }
    public string CultureId { get; set; }

    public CityData(string cityName, int population, string cultureId = "")
    {
        CityName = cityName;
        Population = population;
        CultureId = cultureId;
    }

    public override bool Equals(object obj)
    {
        throw new NotImplementedException();
    }

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }

    public static bool operator ==(CityData left, CityData right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CityData left, CityData right)
    {
        return !(left == right);
    }
}
