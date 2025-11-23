using System.Collections.Generic;
using PigeonPea.Camera2D.Extensions;
using PigeonPea.Camera2D.Math;

namespace PigeonPea.Camera2D.Core;

public sealed class ParallaxExtension : ICameraExtension
{
    public sealed class ParallaxLayer
    {
        public string Name { get; }
        public float SpeedMultiplier { get; set; }

        public ParallaxLayer(string name, float speedMultiplier)
        {
            Name = name;
            SpeedMultiplier = speedMultiplier;
        }
    }

    public string Name => "Parallax";
    public bool Enabled { get; set; } = true;

    private readonly List<ParallaxLayer> _layers = new();
    private Camera2D? _camera;

    public IReadOnlyList<ParallaxLayer> Layers => _layers;

    public void Initialize(Camera2D camera)
    {
        _camera = camera;
    }

    public void AddLayer(string name, float speedMultiplier)
    {
        _layers.Add(new ParallaxLayer(name, speedMultiplier));
    }

    public bool RemoveLayer(string name)
    {
        var index = _layers.FindIndex(l => l.Name == name);
        if (index < 0)
        {
            return false;
        }

        _layers.RemoveAt(index);
        return true;
    }

    public Vector2 GetLayerOffset(ParallaxLayer layer)
    {
        if (_camera == null)
        {
            return Vector2.Zero;
        }

        return _camera.Transform.Position * layer.SpeedMultiplier;
    }

    public void PreUpdate(float deltaTime)
    {
    }

    public void Update(float deltaTime)
    {
    }

    public void PostUpdate(float deltaTime)
    {
    }
}
