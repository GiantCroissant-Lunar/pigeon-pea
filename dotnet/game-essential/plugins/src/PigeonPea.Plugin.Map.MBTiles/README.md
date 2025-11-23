# MBTiles Map Provider & Exporter

This plugin provides support for the [MBTiles](https://github.com/mapbox/mbtiles-spec) format in PigeonPea. It allows you to:

1.  **Export** any `IMapProvider` to an MBTiles file (vector tiles).
2.  **Import** and use MBTiles files as a map source.

## Features

- **Vector Tiles:** Uses Mapbox Vector Tile (MVT) specification (Protobuf encoded).
- **BruTile Integration:** Leverages [BruTile](https://github.com/BruTile/BruTile) for robust tile handling.
- **Compression:** Supports Gzip compression for tile data.
- **Parallel Export:** Generates tiles in parallel for fast export.
- **Metadata:** Preserves map metadata (name, description, attribution).

## Usage

### Importing MBTiles

To use an MBTiles file as a map provider:

```csharp
using PigeonPea.Plugin.Map.MBTiles;

// Create the provider
var provider = new MBTilesMapProvider("path/to/map.mbtiles");

// Get map data for a specific area
var bounds = new BoundingBox(0, 0, 1000, 1000);
var mapData = await provider.GetMapAsync(bounds);

// Render or process features
var features = mapData.GetFeatures(bounds, new ZoomLevel(4));
```

### Exporting to MBTiles

To export a map from another provider (e.g., FMG) to MBTiles:

```csharp
using PigeonPea.Map.Export;
using PigeonPea.Map.Encoding;

// Configure options
var options = new MBTilesOptions
{
    Name = "My Fantasy Map",
    Description = "Generated from FMG",
    Bounds = new BoundingBox(0, 0, 8192, 8192),
    MinZoom = 0,
    MaxZoom = 5,
    MaxParallelism = 8,
    BatchSize = 500
};

// Create dependencies
var encoder = new VectorTileEncoder();
var exporter = new MBTilesExporter(sourceProvider, options, encoder);

// Run export
await exporter.ExportAsync("output.mbtiles");
```

## Compatibility

The generated MBTiles files are compatible with standard tools:

- **QGIS:** Can be opened as a Vector Tile Layer.
- **Mapscii:** Can be viewed in the terminal.

## Architecture

- **PigeonPea.Plugin.Map.MBTiles:** The core plugin containing the `MBTilesMapProvider`.
- **PigeonPea.Map.Export:** Contains the `MBTilesExporter` logic.
- **PigeonPea.Map.Encoding:** Handles Vector Tile (PBF) encoding/decoding.
