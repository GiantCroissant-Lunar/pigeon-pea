using PigeonPea.Camera2D.Core;

namespace NexusCamera2D.Extensions
{
    public interface ICameraExtension
    {
        string Name { get; }
        bool Enabled { get; set; }

        void Initialize(Camera2D camera);
        void PreUpdate(float deltaTime);
        void Update(float deltaTime);
        void PostUpdate(float deltaTime);
    }
}
