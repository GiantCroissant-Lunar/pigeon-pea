using PigeonPea.Shared.Camera2D.Core;

namespace PigeonPea.Shared.Camera2D.Triggers;

public interface ICameraTrigger
{
    string Name { get; }
    bool Enabled { get; set; }

    void Initialize(Camera2DController camera);
    void Update(float deltaTime);
}
