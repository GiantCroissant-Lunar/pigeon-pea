using Arch.Core;
using FluentAssertions;
using PigeonPea.Shared.Components;
using PigeonPea.Shared.ViewModels;
using SadRogue.Primitives;
using Xunit;

namespace PigeonPea.Shared.Tests.ViewModels;

/// <summary>
/// Tests for PlayerViewModel to verify property notifications and ECS synchronization.
/// </summary>
public class PlayerViewModelTests : IDisposable
{
    private readonly World _world;
    private readonly Entity _playerEntity;

    public PlayerViewModelTests()
    {
        _world = World.Create();
        _playerEntity = _world.Create(
            new Position(new Point(10, 20)),
            new PlayerComponent { Name = "TestHero" },
            new Health { Current = 75, Maximum = 100 },
            new Experience { Level = 5, CurrentXP = 250, XPToNextLevel = 500 }
        );
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void Health_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Health))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Health = 50;

        // Assert
        propertyChanged.Should().BeTrue("Health property change should raise PropertyChanged");
        viewModel.Health.Should().Be(50);
    }

    [Fact]
    public void MaxHealth_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.MaxHealth))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.MaxHealth = 150;

        // Assert
        propertyChanged.Should().BeTrue("MaxHealth property change should raise PropertyChanged");
        viewModel.MaxHealth.Should().Be(150);
    }

    [Fact]
    public void Level_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Level))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Level = 10;

        // Assert
        propertyChanged.Should().BeTrue("Level property change should raise PropertyChanged");
        viewModel.Level.Should().Be(10);
    }

    [Fact]
    public void Experience_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Experience))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Experience = 500;

        // Assert
        propertyChanged.Should().BeTrue("Experience property change should raise PropertyChanged");
        viewModel.Experience.Should().Be(500);
    }

    [Fact]
    public void Name_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Name))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Name = "NewHero";

        // Assert
        propertyChanged.Should().BeTrue("Name property change should raise PropertyChanged");
        viewModel.Name.Should().Be("NewHero");
    }

    [Fact]
    public void Position_PropertyChanged_RaisesNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Position))
            {
                propertyChanged = true;
            }
        };

        // Act
        viewModel.Position = new Point(5, 10);

        // Assert
        propertyChanged.Should().BeTrue("Position property change should raise PropertyChanged");
        viewModel.Position.Should().Be(new Point(5, 10));
    }

    [Fact]
    public void HealthDisplay_ReturnsFormattedString()
    {
        // Arrange
        var viewModel = new PlayerViewModel
        {
            Health = 75,
            MaxHealth = 100
        };

        // Act
        var display = viewModel.HealthDisplay;

        // Assert
        display.Should().Be("75/100");
    }

    [Fact]
    public void HealthPercentage_CalculatesCorrectly()
    {
        // Arrange
        var viewModel = new PlayerViewModel
        {
            Health = 75,
            MaxHealth = 100
        };

        // Act
        var percentage = viewModel.HealthPercentage;

        // Assert
        percentage.Should().Be(0.75);
    }

    [Fact]
    public void HealthPercentage_WithZeroMaxHealth_ReturnsZero()
    {
        // Arrange
        var viewModel = new PlayerViewModel
        {
            Health = 50,
            MaxHealth = 0
        };

        // Act
        var percentage = viewModel.HealthPercentage;

        // Assert
        percentage.Should().Be(0.0);
    }

    [Fact]
    public void LevelDisplay_ReturnsFormattedString()
    {
        // Arrange
        var viewModel = new PlayerViewModel
        {
            Level = 5
        };

        // Act
        var display = viewModel.LevelDisplay;

        // Assert
        display.Should().Be("Level 5");
    }

    [Fact]
    public void Update_SyncsHealthFromECS()
    {
        // Arrange
        var viewModel = new PlayerViewModel();

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Health.Should().Be(75);
        viewModel.MaxHealth.Should().Be(100);
    }

    [Fact]
    public void Update_SyncsExperienceFromECS()
    {
        // Arrange
        var viewModel = new PlayerViewModel();

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Level.Should().Be(5);
        viewModel.Experience.Should().Be(250);
    }

    [Fact]
    public void Update_SyncsNameFromECS()
    {
        // Arrange
        var viewModel = new PlayerViewModel();

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Name.Should().Be("TestHero");
    }

    [Fact]
    public void Update_SyncsPositionFromECS()
    {
        // Arrange
        var viewModel = new PlayerViewModel();

        // Act
        viewModel.Update(_world, _playerEntity);

        // Assert
        viewModel.Position.Should().Be(new Point(10, 20));
    }

    [Fact]
    public void Update_WithDeadEntity_DoesNotThrow()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        var deadWorld = World.Create();
        var deadEntity = deadWorld.Create();
        deadWorld.Destroy(deadEntity);

        // Act
        Action act = () => viewModel.Update(deadWorld, deadEntity);

        // Assert
        act.Should().NotThrow("Update should handle dead entities gracefully");

        World.Destroy(deadWorld);
    }

    [Fact]
    public void Update_WithMissingComponents_DoesNotThrow()
    {
        // Arrange
        var viewModel = new PlayerViewModel
        {
            Health = 50,
            MaxHealth = 100,
            Level = 3,
            Experience = 150,
            Name = "OldName",
            Position = new Point(5, 5)
        };

        var emptyWorld = World.Create();
        var emptyEntity = emptyWorld.Create(); // Entity with no components

        // Act
        Action act = () => viewModel.Update(emptyWorld, emptyEntity);

        // Assert
        act.Should().NotThrow("Update should handle missing components gracefully");
        // Properties should remain unchanged since no components exist
        viewModel.Health.Should().Be(50);
        viewModel.MaxHealth.Should().Be(100);

        World.Destroy(emptyWorld);
    }

    [Fact]
    public void Update_MultipleUpdates_SyncsCorrectly()
    {
        // Arrange
        var viewModel = new PlayerViewModel();

        // Act - First update
        viewModel.Update(_world, _playerEntity);
        var firstHealth = viewModel.Health;

        // Modify entity health
        ref var health = ref _world.Get<Health>(_playerEntity);
        health.Current = 50;

        // Act - Second update
        viewModel.Update(_world, _playerEntity);

        // Assert
        firstHealth.Should().Be(75, "First update should sync initial health");
        viewModel.Health.Should().Be(50, "Second update should sync modified health");
    }

    [Fact]
    public void PropertyChanged_DoesNotFireWhenValueUnchanged()
    {
        // Arrange
        var viewModel = new PlayerViewModel { Health = 100 };
        int changeCount = 0;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.Health))
            {
                changeCount++;
            }
        };

        // Act
        viewModel.Health = 100; // Same value

        // Assert
        changeCount.Should().Be(0, "PropertyChanged should not fire when value is unchanged");
    }

    [Fact]
    public void Health_PropertyChanged_RaisesHealthDisplayNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel { MaxHealth = 100 };
        bool healthDisplayChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.HealthDisplay))
            {
                healthDisplayChanged = true;
            }
        };

        // Act
        viewModel.Health = 75;

        // Assert
        healthDisplayChanged.Should().BeTrue("HealthDisplay should be notified when Health changes");
    }

    [Fact]
    public void Health_PropertyChanged_RaisesHealthPercentageNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel { MaxHealth = 100 };
        bool healthPercentageChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.HealthPercentage))
            {
                healthPercentageChanged = true;
            }
        };

        // Act
        viewModel.Health = 75;

        // Assert
        healthPercentageChanged.Should().BeTrue("HealthPercentage should be notified when Health changes");
    }

    [Fact]
    public void MaxHealth_PropertyChanged_RaisesHealthDisplayNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel { Health = 75 };
        bool healthDisplayChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.HealthDisplay))
            {
                healthDisplayChanged = true;
            }
        };

        // Act
        viewModel.MaxHealth = 150;

        // Assert
        healthDisplayChanged.Should().BeTrue("HealthDisplay should be notified when MaxHealth changes");
    }

    [Fact]
    public void MaxHealth_PropertyChanged_RaisesHealthPercentageNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel { Health = 75 };
        bool healthPercentageChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.HealthPercentage))
            {
                healthPercentageChanged = true;
            }
        };

        // Act
        viewModel.MaxHealth = 150;

        // Assert
        healthPercentageChanged.Should().BeTrue("HealthPercentage should be notified when MaxHealth changes");
    }

    [Fact]
    public void Level_PropertyChanged_RaisesLevelDisplayNotification()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool levelDisplayChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == nameof(PlayerViewModel.LevelDisplay))
            {
                levelDisplayChanged = true;
            }
        };

        // Act
        viewModel.Level = 10;

        // Assert
        levelDisplayChanged.Should().BeTrue("LevelDisplay should be notified when Level changes");
    }
}
