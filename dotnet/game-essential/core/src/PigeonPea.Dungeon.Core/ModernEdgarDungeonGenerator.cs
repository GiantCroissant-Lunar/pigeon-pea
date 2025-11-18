using System;
using System.Collections.Generic;
using System.Linq;
using Edgar.Geometry;
using Edgar.GraphBasedGenerator.Grid2D;
using Edgar.Legacy.GeneralAlgorithms.DataStructures.Common;
using Edgar.Legacy.Utils.Interfaces;

namespace PigeonPea.Dungeon.Core;

/// <summary>
/// Dungeon generator backed by the modern Edgar-DotNet graph-based generator.
/// Produces a DungeonData grid that can be consumed by GameWorld via IDungeonGenerator.
/// </summary>
public sealed class ModernEdgarDungeonGenerator : IDungeonGenerator
{
    public DungeonData Generate(int width, int height, int? seed = null)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException("width/height must be positive.");
        }

        var levelDescription = BuildLevelDescription();
        var generator = new GraphBasedGeneratorGrid2D<int>(levelDescription);

        // Optional deterministic seed support
        if (seed.HasValue && generator is IRandomInjectable randomInjectable)
        {
            randomInjectable.InjectRandomGenerator(new Random(seed.Value));
        }

        var layout = generator.GenerateLayout();
        return RasterizeLayout(layout, width, height);
    }

    private static LevelDescriptionGrid2D<int> BuildLevelDescription()
    {
        // Basic rectangular and square room templates similar to BasicsExample.
        var squareOutline = PolygonGrid2D.GetSquare(8);
        var rectangleOutline = PolygonGrid2D.GetRectangle(6, 10);

        var doors = new SimpleDoorModeGrid2D(doorLength: 1, cornerDistance: 1);

        var transformations = new List<TransformationGrid2D>
        {
            TransformationGrid2D.Identity,
            TransformationGrid2D.Rotate90
        };

        var rectangleRoomTemplate = new RoomTemplateGrid2D(
            rectangleOutline,
            doors,
            name: "Rectangle 6x10",
            allowedTransformations: transformations);

        var squareRoomTemplate = new RoomTemplateGrid2D(
            squareOutline,
            new SimpleDoorModeGrid2D(doorLength: 1, cornerDistance: 1),
            name: "Square 8x8");

        var roomDescription = new RoomDescriptionGrid2D(
            isCorridor: false,
            roomTemplates: new List<RoomTemplateGrid2D> { rectangleRoomTemplate, squareRoomTemplate });

        var levelDescription = new LevelDescriptionGrid2D<int>();

        // Simple small graph of rooms; enough to exercise the generator.
        levelDescription.AddRoom(0, roomDescription);
        levelDescription.AddRoom(1, roomDescription);
        levelDescription.AddRoom(2, roomDescription);
        levelDescription.AddRoom(3, roomDescription);
        levelDescription.AddRoom(4, roomDescription);

        levelDescription.AddConnection(0, 1);
        levelDescription.AddConnection(0, 3);
        levelDescription.AddConnection(0, 4);
        levelDescription.AddConnection(1, 2);
        levelDescription.AddConnection(2, 3);

        return levelDescription;
    }

    private static DungeonData RasterizeLayout(LayoutGrid2D<int> layout, int width, int height)
    {
        var dungeon = new DungeonData(width, height);

        // Start with solid walls everywhere.
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                dungeon.SetWall(x, y);
            }
        }

        if (layout.Rooms == null || layout.Rooms.Count == 0)
        {
            return dungeon;
        }

        // Compute world-space bounds of all rooms.
        var worldRects = new List<RectangleGrid2D>();
        int minX = int.MaxValue, minY = int.MaxValue;
        int maxX = int.MinValue, maxY = int.MinValue;

        foreach (var room in layout.Rooms)
        {
            // Outline is in local space; Position is the room offset.
            var rect = room.Outline.BoundingRectangle + room.Position;
            worldRects.Add(rect);

            minX = Math.Min(minX, rect.A.X);
            minY = Math.Min(minY, rect.A.Y);
            maxX = Math.Max(maxX, rect.B.X);
            maxY = Math.Max(maxY, rect.B.Y);
        }

        if (minX == int.MaxValue)
        {
            return dungeon;
        }

        var worldWidth = maxX - minX;
        var worldHeight = maxY - minY;

        // Shift rooms so that the whole layout starts at (0,0) and is roughly centered.
        var baseOffsetX = -minX;
        var baseOffsetY = -minY;

        // Center within target grid if there is space.
        var extraOffsetX = Math.Max(0, (width - worldWidth) / 2);
        var extraOffsetY = Math.Max(0, (height - worldHeight) / 2);

        var totalOffset = new Vector2Int(baseOffsetX + extraOffsetX, baseOffsetY + extraOffsetY);

        foreach (var rect in worldRects)
        {
            var shifted = rect + totalOffset;

            // Fill rectangle interior as floor tiles.
            for (int y = shifted.A.Y; y < shifted.B.Y; y++)
            {
                for (int x = shifted.A.X; x < shifted.B.X; x++)
                {
                    if (dungeon.InBounds(x, y))
                    {
                        dungeon.SetFloor(x, y);
                    }
                }
            }
        }

        return dungeon;
    }
}
