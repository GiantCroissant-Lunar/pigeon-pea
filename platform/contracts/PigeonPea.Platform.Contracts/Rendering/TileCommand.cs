namespace PigeonPea.Platform.Contracts.Rendering;

/// <summary>
/// Command to draw a single tile at a specific position.
/// Used for batching tile draw operations.
/// </summary>
public readonly record struct TileCommand(int X, int Y, Tile Tile);
