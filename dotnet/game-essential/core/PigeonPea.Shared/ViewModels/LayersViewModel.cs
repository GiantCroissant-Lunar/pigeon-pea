using System.Collections.ObjectModel;
using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

public abstract class LayerViewModel : ReactiveObject
{
    public bool IsVisible { get; set; } = true;
}

public class PolylineFeature
{
    public System.ReadOnlyMemory<(double X, double Y)> Points { get; init; }
    public (byte R, byte G, byte B, byte A) Color { get; init; } = (0, 255, 255, 255);
    public float Width { get; init; } = 1f;
}

public class PointFeature
{
    public double X { get; init; }
    public double Y { get; init; }
    public (byte R, byte G, byte B, byte A) Color { get; init; } = (255, 255, 255, 255);
    public float Size { get; init; } = 2f;
}

public class PolylineLayerViewModel : LayerViewModel
{
    public ObservableCollection<PolylineFeature> Features { get; } = new();
}

public class PointLayerViewModel : LayerViewModel
{
    public ObservableCollection<PointFeature> Features { get; } = new();
}

/// <summary>
/// Container of overlay layers for the map. These are internal/in-memory layers;
/// desktop may additionally render vector layers on top if desired.
/// </summary>
public class LayersViewModel : ReactiveObject
{
    public ObservableCollection<LayerViewModel> Layers { get; } = new();
}

