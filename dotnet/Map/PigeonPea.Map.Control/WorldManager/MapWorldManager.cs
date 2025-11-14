using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Map.Core;
using PigeonPea.Shared.ECS.Components;
using PigeonPea.Shared.ECS.Components.Tags;

namespace PigeonPea.Map.Control.WorldManager;

/// <summary>
/// Manages the ECS world for map entities (cities, markers, etc.).
/// </summary>
public class MapWorldManager : IDisposable
{
    private readonly World _world;
    private readonly MapData _mapData;

    public World World => _world;

    public MapWorldManager(MapData mapData)
    {
        _world = World.Create();
        _mapData = mapData;
    }

    /// <summary>
    /// Populate cities from MapData.
    /// Assumes MapData has city/burg information (from FantasyMapGenerator).
    /// </summary>
    public void PopulateCitiesFromMapData()
    {
        // NOTE: This depends on FantasyMapGenerator's burg data structure
        // For now, this is a placeholder showing the pattern
        // You'll need to access actual burg data from MapData

        // Example: If MapData exposes Burgs collection
        // foreach (var burg in _mapData.Burgs)
        // {
        //     CreateCity(burg.X, burg.Y, burg.Name, burg.Population);
        // }

        // Placeholder: Create a few example cities
        CreateCity(100, 100, "Capital City", 50000, "default");
        CreateCity(200, 150, "Port Town", 20000, "coastal");
        CreateCity(150, 200, "Mountain Fortress", 10000, "highland");
    }

    /// <summary>
    /// Create a city entity.
    /// </summary>
    public Entity CreateCity(int x, int y, string cityName, int population, string cultureId = "")
    {
        var entity = _world.Create<Position, Sprite, CityData, Name, MapEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite("city", '◉', 255, 200, 100)); // Gold circle
        _world.Set(entity, new CityData(cityName, population, cultureId));
        _world.Set(entity, new Name(cityName));
        _world.Set(entity, new MapEntityTag());
        _world.Set(entity, new Renderable(true, Layer: 10));

        return entity;
    }

    /// <summary>
    /// Create a map marker (point of interest).
    /// </summary>
    public Entity CreateMarker(int x, int y, string markerType, string title, bool discovered = false)
    {
        (char ch, byte r, byte g, byte b) = markerType.ToLowerInvariant() switch
        {
            "quest" => ('!', (byte)255, (byte)255, (byte)0),   // Yellow
            "dungeon" => ('▼', (byte)200, (byte)100, (byte)100), // Red
            "landmark" => ('△', (byte)100, (byte)200, (byte)255), // Blue
            _ => ('?', (byte)200, (byte)200, (byte)200)
        };

        var entity = _world.Create<Position, Sprite, MarkerData, Name, MapEntityTag, Renderable>();

        _world.Set(entity, new Position(x, y));
        _world.Set(entity, new Sprite(markerType, ch, r, g, b));
        _world.Set(entity, new MarkerData(markerType, title, discovered));
        _world.Set(entity, new Name(title));
        _world.Set(entity, new MapEntityTag());
        _world.Set(entity, new Renderable(discovered, Layer: 5)); // Only visible if discovered

        return entity;
    }

    /// <summary>
    /// Create multiple random dungeon markers.
    /// </summary>
    public void CreateRandomDungeonMarkers(int count, Random? rng = null)
    {
        rng ??= new Random();

        for (int i = 0; i < count; i++)
        {
            // Generate random positions within map bounds
            // NOTE: Adjust based on actual MapData dimensions
            int x = rng.Next(0, 500);
            int y = rng.Next(0, 500);

            bool discovered = rng.NextDouble() < 0.3; // 30% discovered
            CreateMarker(x, y, "dungeon", $"Dungeon {i + 1}", discovered);
        }
    }

    /// <summary>
    /// Get all cities within a viewport.
    /// </summary>
    public List<(Entity entity, Position pos, CityData data)> GetCitiesInViewport(int viewportX, int viewportY, int viewportWidth, int viewportHeight)
    {
        var result = new List<(Entity, Position, CityData)>();
        var query = new QueryDescription().WithAll<Position, CityData, MapEntityTag>();

        _world.Query(in query, (Entity entity, ref Position pos, ref CityData data) =>
        {
            int localX = pos.X - viewportX;
            int localY = pos.Y - viewportY;

            if (localX >= 0 && localX < viewportWidth && localY >= 0 && localY < viewportHeight)
            {
                result.Add((entity, pos, data));
            }
        });

        return result;
    }

    public void Dispose()
    {
        _world.Dispose();
    }
}
