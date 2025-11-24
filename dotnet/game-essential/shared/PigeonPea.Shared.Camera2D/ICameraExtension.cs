using PigeonPea.Shared.Camera2D.Core;

namespace PigeonPea.Shared.Camera2D.Extensions
{
    public interface ICameraExtension
    {
        string Name { get; }
        bool Enabled { get; set; }

        void Initialize(Camera2DController camera);
        void PreUpdate(float deltaTime);
        void Update(float deltaTime);
        void PostUpdate(float deltaTime);
    }
}
