using System;
using System.IO;
using FantasyMapGenerator.Core.Generators;
using FantasyMapGenerator.Core.Models;
using PigeonPea.Shared.Rendering;
using PigeonPea.Map.Rendering;
using PigeonPea.Map.Rendering.Tiles;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PigeonPea.MapsuiHost;

internal static class Program
{
    static void Main(string[] args)
    {
        // Generate a demo map (same defaults as console demo)
        var settings = new MapGenerationSettings { Width = 800, Height = 600, NumPoints = 4000, HeightmapMode = HeightmapMode.Template, Seed = 123456, GridMode = GridMode.Jittered };
        var generator = new MapGenerator();
        var map = generator.Generate(settings);

        // Define a viewport and a skia tile source
        var viewport = new Viewport(0, 0, 120, 80); // world-space cells visible
        var tileSource = new SkiaTileSource();
        int ppc = 8; // pixels per cell
        int tileCols = 40;
        int tileRows = 30;
        int tilesX = (int)Math.Ceiling(viewport.Width / (double)tileCols);
        int tilesY = (int)Math.Ceiling(viewport.Height / (double)tileRows);
        Directory.CreateDirectory("out_tiles");

        for (int ty = 0; ty < tilesY; ty++)
        for (int tx = 0; tx < tilesX; tx++)
        {
            var req = new TileRequest(tx, ty, tileCols, tileRows, ppc);
            var tile = tileSource.GetTile(map, viewport, req, zoom: 1.0, biomeColors: true, rivers: true);
            SavePng(tile.Rgba, tile.WidthPx, tile.HeightPx, Path.Combine("out_tiles", $"z0_{tx}_{ty}.png"));
        }
        Console.WriteLine("Wrote tile PNGs to out_tiles/");
    }

    static void SavePng(byte[] rgba, int w, int h, string path)
    {
        using var img = Image.LoadPixelData<Rgba32>(rgba, w, h);
        img.SaveAsPng(path);
    }
}
