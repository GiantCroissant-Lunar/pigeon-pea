namespace PigeonPea.Map.Core.Adapters;

internal static class FmgSettingsMapper
{
    public static FantasyMapGenerator.Core.Models.MapGenerationSettings ToFmg(MapGenerationSettings s)
        => new FantasyMapGenerator.Core.Models.MapGenerationSettings
        {
            Width = s.Width,
            Height = s.Height,
            NumPoints = s.NumPoints,
            Seed = s.Seed,
            SeedString = s.SeedString,
            ReseedAtPhaseStart = s.ReseedAtPhaseStart,
            GridMode = s.GridMode == PigeonPea.Map.Core.GridMode.Jittered ? FantasyMapGenerator.Core.Models.GridMode.Jittered : FantasyMapGenerator.Core.Models.GridMode.Poisson,
            HeightmapMode = s.HeightmapMode == PigeonPea.Map.Core.HeightmapMode.Template ? FantasyMapGenerator.Core.Models.HeightmapMode.Template : FantasyMapGenerator.Core.Models.HeightmapMode.Auto,
            UseAdvancedNoise = s.UseAdvancedNoise,
            HeightmapTemplate = s.HeightmapTemplate,
            RNGMode = s.RNGMode switch
            {
                PigeonPea.Map.Core.RNGMode.Alea => FantasyMapGenerator.Core.Models.RNGMode.Alea,
                PigeonPea.Map.Core.RNGMode.XorShift => FantasyMapGenerator.Core.Models.RNGMode.Alea,
                PigeonPea.Map.Core.RNGMode.DotNet => FantasyMapGenerator.Core.Models.RNGMode.System,
                _ => FantasyMapGenerator.Core.Models.RNGMode.Alea
            }
        };
}
