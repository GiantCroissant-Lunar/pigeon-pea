using System.Collections.Concurrent;

namespace PigeonPea.Shared.Rendering.Tiles;

public sealed class TileCache
{
    private readonly ConcurrentDictionary<(int x,int y,int cols,int rows,int ppc,double zoom,bool biome,bool rivers,int mapHash), TileImage> _cache = new();

    public bool TryGet((int x,int y,int cols,int rows,int ppc,double zoom,bool biome,bool rivers,int mapHash) key, out TileImage image)
        => _cache.TryGetValue(key, out image!);

    public void Set((int x,int y,int cols,int rows,int ppc,double zoom,bool biome,bool rivers,int mapHash) key, TileImage image)
        => _cache[key] = image;

    public void Clear() => _cache.Clear();
}
