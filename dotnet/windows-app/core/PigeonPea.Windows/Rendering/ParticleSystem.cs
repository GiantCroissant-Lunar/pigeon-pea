using System.Numerics;
using SadRogue.Primitives;
using SkiaSharp;

namespace PigeonPea.Windows.Rendering;

/// <summary>
/// Manages a collection of particles with object pooling for efficient rendering of visual effects.
/// </summary>
public class ParticleSystem
{
    private readonly List<Particle> _particlePool;
    private readonly List<Particle> _activeParticles;
    private readonly Random _random;
    private int _maxParticles;

    /// <summary>
    /// Initializes a new instance of the <see cref="ParticleSystem"/> class.
    /// </summary>
    /// <param name="maxParticles">The maximum number of particles that can be active at once.</param>
    public ParticleSystem(int maxParticles = 1000)
    {
        _maxParticles = maxParticles;
        _particlePool = new List<Particle>(maxParticles);
        _activeParticles = new List<Particle>(maxParticles);
        _random = Random.Shared;

        // Pre-allocate particles in the pool
        for (int i = 0; i < maxParticles; i++)
        {
            _particlePool.Add(new Particle());
        }
    }

    /// <summary>
    /// Gets the count of currently active particles.
    /// </summary>
    public int ActiveParticleCount => _activeParticles.Count;

    /// <summary>
    /// Gets the maximum number of particles.
    /// </summary>
    public int MaxParticles => _maxParticles;

    /// <summary>
    /// Updates all active particles in the system.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update in seconds.</param>
    public void Update(float deltaTime)
    {
        // Update all active particles
        for (int i = _activeParticles.Count - 1; i >= 0; i--)
        {
            var particle = _activeParticles[i];
            particle.Update(deltaTime);

            // Return expired particles to the pool
            if (!particle.IsActive)
            {
                particle.Reset();
                _particlePool.Add(particle);

                // O(1) removal by swapping with the last element before removing.
                // This is safe because we are iterating backwards.
                _activeParticles[i] = _activeParticles[^1];
                _activeParticles.RemoveAt(_activeParticles.Count - 1);
            }
        }
    }

    /// <summary>
    /// Renders all active particles using the provided renderer interface.
    /// This is the preferred method for rendering particles through the abstraction layer.
    /// </summary>
    /// <param name="renderer">The renderer to use for drawing particles.</param>
    /// <param name="tileSize">The size of each tile in pixels.</param>
    public void Render(PigeonPea.Shared.Rendering.IRenderer renderer, int tileSize = 16)
    {
        if (renderer == null)
            throw new ArgumentNullException(nameof(renderer));

        foreach (var particle in _activeParticles)
        {
            // Calculate alpha based on particle age for fade-out effect
            float alpha = 1.0f - particle.Age;
            var color = particle.Color;

            // Create a color with alpha applied
            var particleColor = new Color(
                color.R,
                color.G,
                color.B,
                (byte)(color.A * alpha));

            // Convert pixel position to grid coordinates
            int gridX = (int)(particle.Position.X / tileSize);
            int gridY = (int)(particle.Position.Y / tileSize);

            // Draw particle as a small dot tile
            var tile = new PigeonPea.Shared.Rendering.Tile('·', particleColor, Color.Transparent);
            renderer.DrawTile(gridX, gridY, tile);
        }
    }

    /// <summary>
    /// Renders all active particles to the specified canvas.
    /// This method is provided for backward compatibility and specialized rendering.
    /// Prefer using Render(IRenderer) for consistent rendering abstraction.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to render to.</param>
    /// <param name="tileSize">The size of each tile/particle in pixels.</param>
    public void Render(SKCanvas canvas, int tileSize = 16)
    {
        if (canvas == null)
            throw new ArgumentNullException(nameof(canvas));

        using var paint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        foreach (var particle in _activeParticles)
        {
            // Calculate alpha based on particle age for fade-out effect
            float alpha = 1.0f - particle.Age;
            var color = particle.Color;
            paint.Color = new SKColor(
                color.R,
                color.G,
                color.B,
                (byte)(color.A * alpha));

            // Draw particle as a small circle
            var pixelX = particle.Position.X;
            var pixelY = particle.Position.Y;
            var radius = tileSize / 4f;

            canvas.DrawCircle(pixelX, pixelY, radius, paint);
        }
    }

    /// <summary>
    /// Emits particles from the specified emitter.
    /// </summary>
    /// <param name="emitter">The emitter configuration to use.</param>
    /// <param name="deltaTime">The time elapsed since the last emit in seconds.</param>
    public void Emit(ParticleEmitter emitter, float deltaTime)
    {
        if (emitter == null)
            throw new ArgumentNullException(nameof(emitter));

        if (!emitter.IsActive || emitter.Rate <= 0)
            return;

        // Calculate how many particles to emit this frame
        emitter.AccumulatedTime += deltaTime;
        float timePerParticle = 1.0f / emitter.Rate;
        int particlesToEmit = (int)(emitter.AccumulatedTime / timePerParticle);

        if (particlesToEmit > 0)
        {
            emitter.AccumulatedTime -= particlesToEmit * timePerParticle;

            // Emit particles
            for (int i = 0; i < particlesToEmit; i++)
            {
                if (_particlePool.Count == 0)
                    break; // No more particles available in the pool

                // Get a particle from the pool
                var particle = _particlePool[_particlePool.Count - 1];
                _particlePool.RemoveAt(_particlePool.Count - 1);

                // Initialize particle properties
                particle.Position = emitter.Position;
                particle.InitialLifetime = Lerp(emitter.MinLifetime, emitter.MaxLifetime, (float)_random.NextDouble());
                particle.Lifetime = particle.InitialLifetime;
                particle.Color = emitter.Color;

                // Calculate random velocity within the spread cone
                float spreadRad = emitter.Spread * (float)Math.PI / 180f;
                float directionRad = emitter.Direction * (float)Math.PI / 180f;
                float randomAngle = directionRad + ((float)_random.NextDouble() - 0.5f) * spreadRad;
                float velocity = Lerp(emitter.MinVelocity, emitter.MaxVelocity, (float)_random.NextDouble());

                particle.Velocity = new Vector2(
                    (float)(Math.Cos(randomAngle) * velocity),
                    (float)(Math.Sin(randomAngle) * velocity));

                _activeParticles.Add(particle);
            }
        }
    }

    /// <summary>
    /// Clears all active particles and returns them to the pool.
    /// </summary>
    public void Clear()
    {
        foreach (var particle in _activeParticles)
        {
            particle.Reset();
            _particlePool.Add(particle);
        }
        _activeParticles.Clear();
    }

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }
}
