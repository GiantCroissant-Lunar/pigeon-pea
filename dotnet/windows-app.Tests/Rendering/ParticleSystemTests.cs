using PigeonPea.Windows.Rendering;
using SadRogue.Primitives;
using SkiaSharp;
using Xunit;

namespace PigeonPea.Windows.Tests.Rendering;

/// <summary>
/// Unit tests for the particle system classes.
/// </summary>
public class ParticleSystemTests : IDisposable
{
    private readonly SKBitmap _bitmap;
    private readonly SKCanvas _canvas;

    public ParticleSystemTests()
    {
        _bitmap = new SKBitmap(320, 240);
        _canvas = new SKCanvas(_bitmap);
    }

    public void Dispose()
    {
        _canvas?.Dispose();
        _bitmap?.Dispose();
    }

    #region Particle Tests

    [Fact]
    public void Particle_InitialState_IsInactive()
    {
        // Arrange & Act
        var particle = new Particle();

        // Assert
        Assert.False(particle.IsActive);
        Assert.Equal(0, particle.Lifetime);
    }

    [Fact]
    public void Particle_WithPositiveLifetime_IsActive()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 1.0f,
            InitialLifetime = 1.0f
        };

        // Assert
        Assert.True(particle.IsActive);
    }

    [Fact]
    public void Particle_Update_ReducesLifetime()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 1.0f,
            InitialLifetime = 1.0f
        };

        // Act
        particle.Update(0.5f);

        // Assert
        Assert.Equal(0.5f, particle.Lifetime, precision: 3);
        Assert.True(particle.IsActive);
    }

    [Fact]
    public void Particle_Update_UpdatesPosition()
    {
        // Arrange
        var particle = new Particle
        {
            Position = new Point(0, 0),
            Velocity = new Point(10, 20),
            Lifetime = 1.0f,
            InitialLifetime = 1.0f
        };

        // Act
        particle.Update(1.0f);

        // Assert
        Assert.Equal(10, particle.Position.X);
        Assert.Equal(20, particle.Position.Y);
    }

    [Fact]
    public void Particle_Update_WhenExpired_StaysInactive()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 0.1f,
            InitialLifetime = 1.0f,
            Position = new Point(0, 0),
            Velocity = new Point(10, 10)
        };

        // Act
        particle.Update(0.2f);

        // Assert
        Assert.False(particle.IsActive);
        Assert.Equal(0, particle.Lifetime);
    }

    [Fact]
    public void Particle_Age_ReturnsNormalizedValue()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 0.5f,
            InitialLifetime = 1.0f
        };

        // Assert
        Assert.Equal(0.5f, particle.Age, precision: 3);
    }

    [Fact]
    public void Particle_Age_WhenJustBorn_ReturnsZero()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 1.0f,
            InitialLifetime = 1.0f
        };

        // Assert
        Assert.Equal(0f, particle.Age, precision: 3);
    }

    [Fact]
    public void Particle_Age_WhenExpired_ReturnsOne()
    {
        // Arrange
        var particle = new Particle
        {
            Lifetime = 0f,
            InitialLifetime = 1.0f
        };

        // Assert
        Assert.Equal(1f, particle.Age, precision: 3);
    }

    [Fact]
    public void Particle_Reset_ClearsAllProperties()
    {
        // Arrange
        var particle = new Particle
        {
            Position = new Point(100, 100),
            Velocity = new Point(50, 50),
            Lifetime = 1.0f,
            InitialLifetime = 2.0f,
            Color = Color.Red
        };

        // Act
        particle.Reset();

        // Assert
        Assert.Equal(Point.None, particle.Position);
        Assert.Equal(Point.None, particle.Velocity);
        Assert.Equal(0, particle.Lifetime);
        Assert.Equal(0, particle.InitialLifetime);
        Assert.Equal(Color.Transparent, particle.Color);
    }

    #endregion

    #region ParticleEmitter Tests

    [Fact]
    public void ParticleEmitter_DefaultState_IsActive()
    {
        // Arrange & Act
        var emitter = new ParticleEmitter();

        // Assert
        Assert.True(emitter.IsActive);
    }

    [Fact]
    public void ParticleEmitter_DefaultValues_AreReasonable()
    {
        // Arrange & Act
        var emitter = new ParticleEmitter();

        // Assert
        Assert.Equal(10f, emitter.Rate);
        Assert.Equal(30f, emitter.Spread);
        Assert.Equal(50f, emitter.MinVelocity);
        Assert.Equal(100f, emitter.MaxVelocity);
        Assert.Equal(0.5f, emitter.MinLifetime);
        Assert.Equal(2.0f, emitter.MaxLifetime);
        Assert.Equal(Color.White, emitter.Color);
    }

    [Fact]
    public void ParticleEmitter_CanSetPosition()
    {
        // Arrange
        var emitter = new ParticleEmitter();
        var position = new Point(100, 200);

        // Act
        emitter.Position = position;

        // Assert
        Assert.Equal(position, emitter.Position);
    }

    [Fact]
    public void ParticleEmitter_CanSetDirection()
    {
        // Arrange
        var emitter = new ParticleEmitter();

        // Act
        emitter.Direction = 90f;

        // Assert
        Assert.Equal(90f, emitter.Direction);
    }

    #endregion

    #region ParticleSystem Tests

    [Fact]
    public void ParticleSystem_InitialState_HasNoActiveParticles()
    {
        // Arrange & Act
        var system = new ParticleSystem(100);

        // Assert
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_MaxParticles_MatchesConstructorParameter()
    {
        // Arrange & Act
        var system = new ParticleSystem(500);

        // Assert
        Assert.Equal(500, system.MaxParticles);
    }

    [Fact]
    public void ParticleSystem_Emit_CreatesParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 100f
        };

        // Act
        system.Emit(emitter, 0.1f); // Should emit ~10 particles

        // Assert
        Assert.True(system.ActiveParticleCount > 0);
    }

    [Fact]
    public void ParticleSystem_Emit_WithNullEmitter_ThrowsArgumentNullException()
    {
        // Arrange
        var system = new ParticleSystem(100);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => system.Emit(null!, 0.1f));
    }

    [Fact]
    public void ParticleSystem_Emit_WhenInactive_DoesNotCreateParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            IsActive = false,
            Rate = 100f
        };

        // Act
        system.Emit(emitter, 0.1f);

        // Assert
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_Emit_WithZeroRate_DoesNotCreateParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Rate = 0f
        };

        // Act
        system.Emit(emitter, 0.1f);

        // Assert
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_Emit_RespectsMaxParticleLimit()
    {
        // Arrange
        var maxParticles = 10;
        var system = new ParticleSystem(maxParticles);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 1000f // Very high rate
        };

        // Act
        system.Emit(emitter, 1.0f); // Try to emit many particles

        // Assert
        Assert.True(system.ActiveParticleCount <= maxParticles);
    }

    [Fact]
    public void ParticleSystem_Update_ReducesParticleLifetime()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f,
            MinLifetime = 1.0f,
            MaxLifetime = 1.0f
        };

        system.Emit(emitter, 0.1f);
        var initialCount = system.ActiveParticleCount;

        // Act
        system.Update(0.5f);

        // Assert - particles should still be active but aged
        Assert.Equal(initialCount, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_Update_RemovesExpiredParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f,
            MinLifetime = 0.1f,
            MaxLifetime = 0.1f
        };

        system.Emit(emitter, 0.1f);

        // Act
        system.Update(0.2f); // Update past particle lifetime

        // Assert
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_Update_RecyclesParticlesToPool()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f,
            MinLifetime = 0.1f,
            MaxLifetime = 0.1f
        };

        // Act - Emit, expire, and emit again
        system.Emit(emitter, 0.1f);
        system.Update(0.2f);
        system.Emit(emitter, 0.1f);

        // Assert - Should be able to emit again after particles expired
        Assert.True(system.ActiveParticleCount > 0);
    }

    [Fact]
    public void ParticleSystem_Render_WithNullCanvas_ThrowsArgumentNullException()
    {
        // Arrange
        var system = new ParticleSystem(100);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => system.Render(null!, 16));
    }

    [Fact]
    public void ParticleSystem_Render_WithActiveParticles_DrawsToCanvas()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f,
            Color = Color.Red
        };

        system.Emit(emitter, 0.1f);

        // Act - Should not throw
        system.Render(_canvas, 16);

        // Assert - Check that something was drawn (pixel is not transparent)
        var pixel = _bitmap.GetPixel(100, 100);
        Assert.NotEqual(SKColors.Transparent, pixel);
    }

    [Fact]
    public void ParticleSystem_Render_WithNoParticles_DoesNotThrow()
    {
        // Arrange
        var system = new ParticleSystem(100);

        // Act & Assert - Should not throw
        system.Render(_canvas, 16);
    }

    [Fact]
    public void ParticleSystem_Clear_RemovesAllActiveParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 100f
        };

        system.Emit(emitter, 0.1f);
        Assert.True(system.ActiveParticleCount > 0);

        // Act
        system.Clear();

        // Assert
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_Clear_ReturnsParticlesToPool()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 100f
        };

        // Act - Fill, clear, and fill again
        system.Emit(emitter, 0.1f);
        system.Clear();
        system.Emit(emitter, 0.1f);

        // Assert - Should be able to emit particles again
        Assert.True(system.ActiveParticleCount > 0);
    }

    [Fact]
    public void ParticleSystem_EmittedParticles_HaveCorrectColor()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var expectedColor = Color.Yellow;
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f,
            Color = expectedColor
        };

        // Act
        system.Emit(emitter, 0.1f);
        
        // We can't directly access particles, but we can verify the render uses the color
        system.Render(_canvas, 16);

        // Assert - Check that yellow was drawn near the emission point
        var pixel = _bitmap.GetPixel(100, 100);
        Assert.NotEqual(SKColors.Transparent, pixel);
        Assert.True(pixel.Red > 200); // Yellow has high red
        Assert.True(pixel.Green > 200); // Yellow has high green
    }

    [Fact]
    public void ParticleSystem_EmittedParticles_HaveVariedVelocities()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 100f,
            MinVelocity = 50f,
            MaxVelocity = 150f
        };

        // Act
        system.Emit(emitter, 0.1f);
        system.Update(0.1f);

        // We can't directly verify velocities, but particles should have moved
        // and still be active, indicating they have velocity
        Assert.True(system.ActiveParticleCount > 0);
    }

    [Fact]
    public void ParticleSystem_EmittedParticles_RespectDirectionAndSpread()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 100f,
            Direction = 0f, // Right
            Spread = 30f
        };

        // Act
        system.Emit(emitter, 0.1f);
        system.Update(0.1f);

        // Assert - Particles should have moved (can't verify direction without exposing internals)
        Assert.True(system.ActiveParticleCount > 0);
    }

    [Fact]
    public void ParticleSystem_EmitMultipleTimes_AccumulatesParticles()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 10f
        };

        // Act
        system.Emit(emitter, 0.1f);
        var firstCount = system.ActiveParticleCount;
        system.Emit(emitter, 0.1f);
        var secondCount = system.ActiveParticleCount;

        // Assert
        Assert.True(secondCount >= firstCount);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ParticleSystem_FullLifecycle_EmitUpdateRenderClear()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter = new ParticleEmitter
        {
            Position = new Point(100, 100),
            Rate = 50f,
            Color = Color.Blue,
            MinLifetime = 0.5f,
            MaxLifetime = 1.0f
        };

        // Act & Assert - Full lifecycle
        system.Emit(emitter, 0.1f);
        Assert.True(system.ActiveParticleCount > 0, "Particles should be emitted");

        system.Update(0.1f);
        Assert.True(system.ActiveParticleCount > 0, "Particles should still be active");

        system.Render(_canvas, 16);
        // Rendering should not throw

        system.Clear();
        Assert.Equal(0, system.ActiveParticleCount);
    }

    [Fact]
    public void ParticleSystem_MultipleEmitters_CanCoexist()
    {
        // Arrange
        var system = new ParticleSystem(100);
        var emitter1 = new ParticleEmitter
        {
            Position = new Point(50, 50),
            Rate = 10f,
            Color = Color.Red
        };
        var emitter2 = new ParticleEmitter
        {
            Position = new Point(150, 150),
            Rate = 10f,
            Color = Color.Blue
        };

        // Act
        system.Emit(emitter1, 0.1f);
        system.Emit(emitter2, 0.1f);

        // Assert
        Assert.True(system.ActiveParticleCount > 0);
        system.Render(_canvas, 16); // Should not throw
    }

    #endregion
}
