using PigeonPea.Windows.Rendering;
using Xunit;

namespace PigeonPea.Windows.Tests.Rendering;

/// <summary>
/// Unit tests for the Animation and AnimationSystem classes.
/// </summary>
public class AnimationSystemTests
{
    #region Animation Tests

    [Fact]
    public void AnimationGetFrameIndexWithZeroElapsedTimeReturnsFirstFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(0f);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexWithElapsedTimeReturnsCorrectFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(0.15f); // Should be on second frame

        // Assert
        Assert.Equal(1, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexLoopingAnimationWrapsAround()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act - Total duration is 0.3s, so 0.35s should wrap to frame 0
        var frameIndex = animation.GetFrameIndex(0.35f);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexNonLoopingAnimationClampsToEnd()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = false
        };

        // Act - Past the end of animation
        var frameIndex = animation.GetFrameIndex(0.5f);

        // Assert
        Assert.Equal(-1, frameIndex); // Animation complete
    }

    [Fact]
    public void AnimationGetCurrentFrameReturnsCorrectSpriteId()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var spriteId = animation.GetCurrentFrame(0.15f);

        // Assert
        Assert.Equal(101, spriteId);
    }

    [Fact]
    public void AnimationGetCurrentFrameWhenCompleteReturnsNull()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = false
        };

        // Act - Past the end of animation
        var spriteId = animation.GetCurrentFrame(0.5f);

        // Assert
        Assert.Null(spriteId);
    }

    [Fact]
    public void AnimationIsCompleteLoopingAnimationReturnsFalse()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var isComplete = animation.IsComplete(10f); // Long time elapsed

        // Assert
        Assert.False(isComplete);
    }

    [Fact]
    public void AnimationIsCompleteNonLoopingAnimationReturnsTrue()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = false
        };

        // Act
        var isComplete = animation.IsComplete(0.5f);

        // Assert
        Assert.True(isComplete);
    }

    [Fact]
    public void AnimationTotalDurationCalculatesCorrectly()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102, 103],
            FrameDuration = 0.25f,
            IsLooping = true
        };

        // Act
        var totalDuration = animation.TotalDuration;

        // Assert
        Assert.Equal(1.0f, totalDuration);
    }

    [Fact]
    public void AnimationWithEmptyFramesGetFrameIndexReturnsNegative()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(0.5f);

        // Assert
        Assert.Equal(-1, frameIndex);
    }

    [Fact]
    public void AnimationWithZeroFrameDurationReturnsFirstFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(10f);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexWithNegativeElapsedTimeReturnsFirstFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(-1f);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexWithNaNElapsedTimeReturnsFirstFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(float.NaN);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    [Fact]
    public void AnimationGetFrameIndexWithInfinityElapsedTimeReturnsFirstFrame()
    {
        // Arrange
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        var frameIndex = animation.GetFrameIndex(float.PositiveInfinity);

        // Assert
        Assert.Equal(0, frameIndex);
    }

    #endregion

    #region AnimationSystem Tests

    [Fact]
    public void AnimationSystemInitialStateHasNoAnimations()
    {
        // Arrange & Act
        var system = new AnimationSystem();

        // Assert
        Assert.Equal(0, system.AnimationCount);
        Assert.Equal(0, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemRegisterAnimationAddsAnimation()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };

        // Act
        system.RegisterAnimation(animation);

        // Assert
        Assert.Equal(1, system.AnimationCount);
        Assert.True(system.HasAnimation(1));
    }

    [Fact]
    public void AnimationSystemRegisterAnimationWithNullAnimationThrowsArgumentNullException()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => system.RegisterAnimation(null!));
    }

    [Fact]
    public void AnimationSystemRegisterAnimationWithDuplicateIdThrowsInvalidOperationException()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation1 = new Animation
        {
            Id = 1,
            Frames = [100, 101],
            FrameDuration = 0.1f
        };
        var animation2 = new Animation
        {
            Id = 1,
            Frames = [200, 201],
            FrameDuration = 0.2f
        };

        system.RegisterAnimation(animation1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => system.RegisterAnimation(animation2));
    }

    [Fact]
    public void AnimationSystemHasAnimationWithUnregisteredIdReturnsFalse()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act & Assert
        Assert.False(system.HasAnimation(999));
    }

    [Fact]
    public void AnimationSystemPlayAnimationReturnsValidInstanceId()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);

        // Act
        var instanceId = system.PlayAnimation(1);

        // Assert
        Assert.True(instanceId > 0);
        Assert.Equal(1, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemPlayAnimationWithUnregisteredIdReturnsNegative()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act
        var instanceId = system.PlayAnimation(999);

        // Assert
        Assert.Equal(-1, instanceId);
        Assert.Equal(0, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemPlayAnimationMultipleTimesCreatesMultipleInstances()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);

        // Act
        var instanceId1 = system.PlayAnimation(1);
        var instanceId2 = system.PlayAnimation(1);

        // Assert
        Assert.NotEqual(instanceId1, instanceId2);
        Assert.Equal(2, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemStopAnimationRemovesInstance()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        var stopped = system.StopAnimation(instanceId);

        // Assert
        Assert.True(stopped);
        Assert.Equal(0, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemStopAnimationWithInvalidIdReturnsFalse()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act
        var stopped = system.StopAnimation(999);

        // Assert
        Assert.False(stopped);
    }

    [Fact]
    public void AnimationSystemUpdateAdvancesElapsedTime()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(0.15f);

        // Assert
        var elapsedTime = system.GetElapsedTime(instanceId);
        Assert.NotNull(elapsedTime);
        Assert.Equal(0.15f, elapsedTime.Value, precision: 3);
    }

    [Fact]
    public void AnimationSystemUpdateRemovesCompletedNonLoopingAnimations()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = false
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(0.5f); // Past the end of animation

        // Assert
        Assert.Equal(0, system.ActiveInstanceCount);
        Assert.False(system.IsInstanceActive(instanceId));
    }

    [Fact]
    public void AnimationSystemUpdateKeepsLoopingAnimations()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(10f); // Long time elapsed

        // Assert
        Assert.Equal(1, system.ActiveInstanceCount);
        Assert.True(system.IsInstanceActive(instanceId));
    }

    [Fact]
    public void AnimationSystemGetCurrentFrameReturnsCorrectSpriteId()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(0.15f); // Should be on second frame
        var spriteId = system.GetCurrentFrame(instanceId);

        // Assert
        Assert.Equal(101, spriteId);
    }

    [Fact]
    public void AnimationSystemGetCurrentFrameWithInvalidInstanceIdReturnsNull()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act
        var spriteId = system.GetCurrentFrame(999);

        // Assert
        Assert.Null(spriteId);
    }

    [Fact]
    public void AnimationSystemIsInstanceActiveWithActiveInstanceReturnsTrue()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act & Assert
        Assert.True(system.IsInstanceActive(instanceId));
    }

    [Fact]
    public void AnimationSystemIsInstanceActiveWithInactiveInstanceReturnsFalse()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act & Assert
        Assert.False(system.IsInstanceActive(999));
    }

    [Fact]
    public void AnimationSystemGetElapsedTimeWithActiveInstanceReturnsElapsedTime()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);
        system.Update(0.5f);

        // Act
        var elapsedTime = system.GetElapsedTime(instanceId);

        // Assert
        Assert.NotNull(elapsedTime);
        Assert.Equal(0.5f, elapsedTime.Value, precision: 3);
    }

    [Fact]
    public void AnimationSystemGetElapsedTimeWithInvalidInstanceIdReturnsNull()
    {
        // Arrange
        var system = new AnimationSystem();

        // Act
        var elapsedTime = system.GetElapsedTime(999);

        // Assert
        Assert.Null(elapsedTime);
    }

    [Fact]
    public void AnimationSystemClearInstancesRemovesAllInstances()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);
        system.PlayAnimation(1);
        system.PlayAnimation(1);

        // Act
        system.ClearInstances();

        // Assert
        Assert.Equal(0, system.ActiveInstanceCount);
        Assert.Equal(1, system.AnimationCount); // Animations still registered
    }

    [Fact]
    public void AnimationSystemClearRemovesEverything()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f
        };
        system.RegisterAnimation(animation);
        system.PlayAnimation(1);

        // Act
        system.Clear();

        // Assert
        Assert.Equal(0, system.ActiveInstanceCount);
        Assert.Equal(0, system.AnimationCount);
    }

    [Fact]
    public void AnimationSystemUpdateWithNegativeDeltaTimeDoesNotUpdate()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(-0.5f);

        // Assert - elapsed time should still be 0
        var elapsedTime = system.GetElapsedTime(instanceId);
        Assert.Equal(0f, elapsedTime);
    }

    [Fact]
    public void AnimationSystemUpdateWithNaNDeltaTimeDoesNotUpdate()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(float.NaN);

        // Assert - elapsed time should still be 0
        var elapsedTime = system.GetElapsedTime(instanceId);
        Assert.Equal(0f, elapsedTime);
    }

    [Fact]
    public void AnimationSystemUpdateWithInfinityDeltaTimeDoesNotUpdate()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);
        var instanceId = system.PlayAnimation(1);

        // Act
        system.Update(float.PositiveInfinity);

        // Assert - elapsed time should still be 0
        var elapsedTime = system.GetElapsedTime(instanceId);
        Assert.Equal(0f, elapsedTime);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AnimationSystemFullLifecycleRegisterPlayUpdateStop()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102, 103],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act & Assert - Full lifecycle
        system.RegisterAnimation(animation);
        Assert.Equal(1, system.AnimationCount);

        var instanceId = system.PlayAnimation(1);
        Assert.True(instanceId > 0);
        Assert.True(system.IsInstanceActive(instanceId));

        system.Update(0.15f);
        var frame1 = system.GetCurrentFrame(instanceId);
        Assert.Equal(101, frame1); // Second frame

        system.Update(0.1f); // Total 0.25s
        var frame2 = system.GetCurrentFrame(instanceId);
        Assert.Equal(102, frame2); // Third frame

        system.StopAnimation(instanceId);
        Assert.False(system.IsInstanceActive(instanceId));
    }

    [Fact]
    public void AnimationSystemMultipleAnimationsCanCoexist()
    {
        // Arrange
        var system = new AnimationSystem();
        var waterAnimation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        var fireAnimation = new Animation
        {
            Id = 2,
            Frames = [200, 201, 202, 203],
            FrameDuration = 0.05f,
            IsLooping = true
        };

        // Act
        system.RegisterAnimation(waterAnimation);
        system.RegisterAnimation(fireAnimation);

        var waterInstance = system.PlayAnimation(1);
        var fireInstance = system.PlayAnimation(2);

        system.Update(0.1f);

        // Assert
        Assert.Equal(2, system.AnimationCount);
        Assert.Equal(2, system.ActiveInstanceCount);

        var waterFrame = system.GetCurrentFrame(waterInstance);
        var fireFrame = system.GetCurrentFrame(fireInstance);

        Assert.Equal(101, waterFrame); // Water on second frame (0.1s / 0.1s = frame index 1)
        Assert.Equal(202, fireFrame); // Fire on third frame (0.1s / 0.05s = frame index 2)
    }

    [Fact]
    public void AnimationSystemOneShotAnimationCompletesAutomatically()
    {
        // Arrange
        var system = new AnimationSystem();
        var explosionAnimation = new Animation
        {
            Id = 1,
            Frames = [300, 301, 302],
            FrameDuration = 0.1f,
            IsLooping = false
        };

        // Act
        system.RegisterAnimation(explosionAnimation);
        var instanceId = system.PlayAnimation(1);

        // Initial state
        Assert.True(system.IsInstanceActive(instanceId));
        var frame1 = system.GetCurrentFrame(instanceId);
        Assert.Equal(300, frame1);

        // Update to second frame
        system.Update(0.15f);
        var frame2 = system.GetCurrentFrame(instanceId);
        Assert.Equal(301, frame2);
        Assert.True(system.IsInstanceActive(instanceId));

        // Update past the end
        system.Update(0.3f); // Total 0.45s > 0.3s duration

        // Assert - Animation should be automatically removed
        Assert.False(system.IsInstanceActive(instanceId));
        Assert.Equal(0, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemLoopingAnimationNeverCompletes()
    {
        // Arrange
        var system = new AnimationSystem();
        var waterAnimation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };

        // Act
        system.RegisterAnimation(waterAnimation);
        var instanceId = system.PlayAnimation(1);

        // Update through multiple cycles
        for (int i = 0; i < 10; i++)
        {
            system.Update(0.35f); // More than one cycle
        }

        // Assert - Animation should still be active
        Assert.True(system.IsInstanceActive(instanceId));
        Assert.Equal(1, system.ActiveInstanceCount);
    }

    [Fact]
    public void AnimationSystemMultipleInstancesOfSameAnimationIndependentTiming()
    {
        // Arrange
        var system = new AnimationSystem();
        var animation = new Animation
        {
            Id = 1,
            Frames = [100, 101, 102],
            FrameDuration = 0.1f,
            IsLooping = true
        };
        system.RegisterAnimation(animation);

        // Act - Start two instances at different times
        var instance1 = system.PlayAnimation(1);
        system.Update(0.15f); // Instance1 at 0.15s
        var instance2 = system.PlayAnimation(1);

        // Assert - Instances should have different frames
        var frame1 = system.GetCurrentFrame(instance1);
        var frame2 = system.GetCurrentFrame(instance2);

        Assert.Equal(101, frame1); // Instance1 on second frame
        Assert.Equal(100, frame2); // Instance2 just started
    }

    #endregion
}
