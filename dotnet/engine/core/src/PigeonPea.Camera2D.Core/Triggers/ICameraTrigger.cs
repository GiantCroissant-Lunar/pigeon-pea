using PigeonPea.Camera2D.Core;

namespace PigeonPea.Camera2D.Triggers;

public interface ICameraTrigger
{
    string Name { get; }
    bool Enabled { get; set; }

    void Initialize(Camera2D camera);
    void Update(float deltaTime);
}
