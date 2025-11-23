using PigeonPea.Camera2D.Core;
using PigeonPea.Camera2D.Math;

namespace PigeonPea.Camera2D.Triggers;

public sealed class ZoomTrigger : ICameraTrigger
{
    public string Name { get; }
    public bool Enabled { get; set; } = true;

    public Rect Area { get; set; }
    public float TargetZoom { get; set; }

    private Camera2D? _camera;
    private bool _wasInside;

    public ZoomTrigger(string name, Rect area, float targetZoom)
    {
        Name = name;
        Area = area;
        TargetZoom = targetZoom;
    }

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    public void Update(float deltaTime)
    {
        if (_camera == null || !Enabled)
        {
            return;
        }

        var position = _camera.Transform.Position;
        var inside = Area.Contains(position);

        if (inside && !_wasInside)
        {
            var zoomExtension = _camera.GetExtension<ZoomExtension>();
            zoomExtension?.SetTargetZoom(TargetZoom);
        }

        _wasInside = inside;
    }
}
