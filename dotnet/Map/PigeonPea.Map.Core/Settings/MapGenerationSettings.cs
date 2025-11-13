namespace PigeonPea.Map.Core;

public enum RNGMode { Alea, XorShift, DotNet }
public enum GridMode { Regular, Jittered }
public enum HeightmapMode { Template, FastNoise }

public sealed class MapGenerationSettings
{
    public int Width { get; set; }
    public int Height { get; set; }
    public int NumPoints { get; set; }
    public int Seed { get; set; }
    public string SeedString { get; set; } = "";
    public bool ReseedAtPhaseStart { get; set; }
    public GridMode GridMode { get; set; }
    public HeightmapMode HeightmapMode { get; set; }
    public RNGMode RNGMode { get; set; } = RNGMode.Alea;
    public bool UseAdvancedNoise { get; set; }
    public string HeightmapTemplate { get; set; } = "";
}
