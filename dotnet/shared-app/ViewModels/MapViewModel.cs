using System.Collections.Generic;
using System.Reactive.Linq;
using Arch.Core;
using Arch.Core.Extensions;
using ObservableCollections;
using ReactiveUI;
using SadRogue.Primitives;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.Rendering;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for map state with reactive property change notifications.
/// Tracks map dimensions, camera position, and visible tiles.
/// </summary>
public class MapViewModel : ReactiveObject
{
    private int _width;
    private int _height;
    private Point _cameraPosition;

    /// <summary>
    /// Width of the map in tiles.
    /// </summary>
    public int Width
    {
        get => _width;
        set => this.RaiseAndSetIfChanged(ref _width, value);
    }

    /// <summary>
    /// Height of the map in tiles.
    /// </summary>
    public int Height
    {
        get => _height;
        set => this.RaiseAndSetIfChanged(ref _height, value);
    }

    /// <summary>
    /// Current camera position in world coordinates.
    /// </summary>
    public Point CameraPosition
    {
        get => _cameraPosition;
        set => this.RaiseAndSetIfChanged(ref _cameraPosition, value);
    }

    /// <summary>
    /// Observable list of visible tiles within the current viewport.
    /// </summary>
    public ObservableList<TileViewModel> VisibleTiles { get; }

    private readonly Camera _camera;

    public MapViewModel() : this(new Camera())
    {
    }

    public MapViewModel(Camera camera)
    {
        _camera = camera;
        VisibleTiles = new ObservableList<TileViewModel>();
        _cameraPosition = Point.None;
    }

    /// <summary>
    /// Updates the ViewModel properties from GameWorld.
    /// Synchronizes map dimensions, camera position, and visible tiles.
    /// </summary>
    /// <param name="gameWorld">The game world to sync from.</param>
    public void Update(GameWorld gameWorld)
    {
        using (this.DelayChangeNotifications())
        {
            // Update map dimensions
            Width = gameWorld.Width;
            Height = gameWorld.Height;

            // Update camera to follow player
            if (gameWorld.PlayerEntity.IsAlive())
            {
                _camera.Follow(gameWorld.PlayerEntity);
                _camera.Update(gameWorld.EcsWorld, new Rectangle(0, 0, gameWorld.Width, gameWorld.Height));
                CameraPosition = _camera.Position;
            }

            // Update visible tiles
            UpdateVisibleTiles(gameWorld);
        }
    }

    private void UpdateVisibleTiles(GameWorld gameWorld)
    {
        VisibleTiles.Clear();

        var viewport = _camera.GetViewport();

        // Get player's field of view if available
        HashSet<Point>? playerFov = null;
        if (gameWorld.PlayerEntity.IsAlive())
        {
            if (gameWorld.EcsWorld.TryGet<FieldOfView>(gameWorld.PlayerEntity, out var fov))
            {
                playerFov = fov.VisibleTiles;
            }
        }

        // Query all entities with Position and Renderable components
        var renderableQuery = new QueryDescription().WithAll<Position, Renderable>();
        
        // Create a dictionary to store the topmost entity at each position
        var tilesInViewport = new Dictionary<Point, (Renderable renderable, bool isExplored)>();

        gameWorld.EcsWorld.Query(in renderableQuery, (Entity entity, ref Position pos, ref Renderable rend) =>
        {
            // Only process tiles within viewport
            if (!viewport.Contains(pos.Point.X, pos.Point.Y))
                return;

            var point = pos.Point;
            bool isExplored = entity.Has<Explored>();

            // If entity blocks movement, it has higher priority and overwrites other entities
            if (entity.Has<BlocksMovement>())
            {
                // Preserve explored status from underlying tile if it was already processed
                bool wasExplored = tilesInViewport.TryGetValue(point, out var existing) && existing.isExplored;
                tilesInViewport[point] = (rend, isExplored || wasExplored);
            }
            else if (!tilesInViewport.ContainsKey(point))
            {
                // Only add non-blocking entities if position is not occupied
                tilesInViewport[point] = (rend, isExplored);
            }
        });

        // Create TileViewModels for each position in the viewport
        foreach (var (position, (renderable, isExplored)) in tilesInViewport)
        {
            bool isVisible = playerFov?.Contains(position) ?? false;

            var tileVm = new TileViewModel
            {
                X = position.X,
                Y = position.Y,
                Glyph = renderable.Glyph,
                Foreground = isVisible ? renderable.Foreground : DimColor(renderable.Foreground),
                Background = isVisible ? renderable.Background : DimColor(renderable.Background),
                IsVisible = isVisible,
                IsExplored = isExplored
            };

            VisibleTiles.Add(tileVm);
        }
    }

    private Color DimColor(Color color)
    {
        // Dim the color for explored but not visible tiles
        return new Color((byte)(color.R / 2), (byte)(color.G / 2), (byte)(color.B / 2), color.A);
    }
}
