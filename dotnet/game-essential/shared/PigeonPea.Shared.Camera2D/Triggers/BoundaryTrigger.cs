using PigeonPea.Shared.Camera2D.Core;
using PigeonPea.Shared.Camera2D.Math;

namespace PigeonPea.Shared.Camera2D.Triggers;

public sealed class BoundaryTrigger : ICameraTrigger
{
    public string Name { get; }
    public bool Enabled { get; set; } = true;

    public Rect Area { get; set; }
    public Rect NewLimits { get; set; }

    private Camera2DController? _camera;
    private bool _wasInside;

    public BoundaryTrigger(string name, Rect area, Rect newLimits)
    {
        Name = name;
        Area = area;
        NewLimits = newLimits;
    }

    public void Initialize(Camera2DController camera)
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
            var boundaries = _camera.GetExtension<BoundariesExtension>();
            if (boundaries != null)
            {
                boundaries.Limits = NewLimits;
            }
        }

        _wasInside = inside;
    }
}
