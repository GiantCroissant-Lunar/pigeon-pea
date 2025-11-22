using System.CommandLine;
using PigeonPea.Map.Contracts.Spatial;
using PigeonPea.Map.Core.Adapters;
using PigeonPea.Map.Encoding;
using PigeonPea.Map.Export;
using PigeonPea.Plugin.Map.FMG;

namespace PigeonPea.Map.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        var outputOption = new Option<string>("--output", "Output MBTiles file path") { IsRequired = true };
        var widthOption = new Option<int>("--width", () => 1024, "Map width");
        var heightOption = new Option<int>("--height", () => 1024, "Map height");
        var minZoomOption = new Option<int>("--min-zoom", () => 0, "Minimum zoom level");
        var maxZoomOption = new Option<int>("--max-zoom", () => 5, "Maximum zoom level");
        var compressOption = new Option<bool>("--compress", () => true, "Enable compression");

        var exportCommand = new Command("export-mbtiles", "Export FMG map to MBTiles")
        {
            outputOption,
            widthOption,
            heightOption,
            minZoomOption,
            maxZoomOption,
            compressOption
        };

        exportCommand.SetHandler(async (output, width, height, minZoom, maxZoom, compress) =>
        {
            await ExportMap(output, width, height, minZoom, maxZoom, compress);
        }, outputOption, widthOption, heightOption, minZoomOption, maxZoomOption, compressOption);

        var rootCommand = new RootCommand("Pigeon Pea Map CLI");
        rootCommand.AddCommand(exportCommand);

        return await rootCommand.InvokeAsync(args);
    }

    static async Task ExportMap(string output, int width, int height, int minZoom, int maxZoom, bool compress)
    {
        Console.WriteLine($"Generating map (Size: {width}x{height})...");

        var generator = new FantasyMapGeneratorAdapter();
        var provider = new FmgMapProvider(generator);

        var bounds = new BoundingBox(0, 0, width, height);
        
        Console.WriteLine("Exporting to MBTiles...");
        
        var encoder = new VectorTileEncoder();
        var options = new MBTilesOptions
        {
            Name = "FMG Map",
            Description = $"Generated FMG Map ({width}x{height})",
            Bounds = bounds,
            MinZoom = minZoom,
            MaxZoom = maxZoom,
            Compress = compress,
            DefaultZoom = 2
        };

        var progress = new Progress<ExportProgress>(p =>
        {
            Console.Write($"\rProgress: {p.Percentage:P0} (Zoom {p.CurrentZoom}/{p.MaxZoom}, Tiles {p.TilesProcessed}/{p.TotalTiles})");
        });

        var exporter = new MBTilesExporter(provider, options, encoder, progress);
        await exporter.ExportAsync(output);

        Console.WriteLine("\nExport complete!");
    }
}
