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

    [Theory]
    [MemberData(nameof(GetPropertyChangeNotificationData))]
    public void Property_WhenChanged_RaisesNotification(string propertyName, object newValue)
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        bool propertyChanged = false;
        viewModel.PropertyChanged += (sender, args) =>
        {
            if (args.PropertyName == propertyName)
            {
                propertyChanged = true;
            }
        };

        // Act
        var propertyInfo = typeof(PlayerViewModel).GetProperty(propertyName);
        propertyInfo!.SetValue(viewModel, newValue);

        // Assert
        propertyChanged.Should().BeTrue($"{propertyName} property change should raise PropertyChanged");
        propertyInfo.GetValue(viewModel).Should().Be(newValue);
    }

    public static IEnumerable<object[]> GetPropertyChangeNotificationData()
    {
        yield return new object[] { nameof(PlayerViewModel.Health), 50 };
        yield return new object[] { nameof(PlayerViewModel.MaxHealth), 150 };
        yield return new object[] { nameof(PlayerViewModel.Level), 10 };
        yield return new object[] { nameof(PlayerViewModel.Experience), 500 };
        yield return new object[] { nameof(PlayerViewModel.Name), "NewHero" };
        yield return new object[] { nameof(PlayerViewModel.Position), new Point(5, 10) };
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

    [Theory]
    [MemberData(nameof(GetComputedPropertyData))]
    public void ComputedProperty_ReflectsSourcePropertyChanges(Action<PlayerViewModel> arrange, Action<PlayerViewModel> act, Func<PlayerViewModel, object> getValue, object expectedValue, string description)
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        arrange(viewModel);

        // Act
        act(viewModel);

        // Assert
        getValue(viewModel).Should().Be(expectedValue, description);
    }

    public static IEnumerable<object[]> GetComputedPropertyData()
    {
        yield return new object[]
        {
            (Action<PlayerViewModel>)(vm => vm.MaxHealth = 100),
            (Action<PlayerViewModel>)(vm => vm.Health = 75),
            (Func<PlayerViewModel, object>)(vm => vm.HealthDisplay),
            "75/100",
            "HealthDisplay should reflect Health changes"
        };
        yield return new object[]
        {
            (Action<PlayerViewModel>)(vm => vm.MaxHealth = 100),
            (Action<PlayerViewModel>)(vm => vm.Health = 75),
            (Func<PlayerViewModel, object>)(vm => vm.HealthPercentage),
            0.75,
            "HealthPercentage should reflect Health changes"
        };
        yield return new object[]
        {
            (Action<PlayerViewModel>)(vm => vm.Health = 75),
            (Action<PlayerViewModel>)(vm => vm.MaxHealth = 100),
            (Func<PlayerViewModel, object>)(vm => vm.HealthDisplay),
            "75/100",
            "HealthDisplay should reflect MaxHealth changes"
        };
        yield return new object[]
        {
            (Action<PlayerViewModel>)(vm => vm.Health = 75),
            (Action<PlayerViewModel>)(vm => vm.MaxHealth = 100),
            (Func<PlayerViewModel, object>)(vm => vm.HealthPercentage),
            0.75,
            "HealthPercentage should reflect MaxHealth changes"
        };
        yield return new object[]
        {
            (Action<PlayerViewModel>)(vm => { }),
            (Action<PlayerViewModel>)(vm => vm.Level = 10),
            (Func<PlayerViewModel, object>)(vm => vm.LevelDisplay),
            "Level 10",
            "LevelDisplay should reflect Level changes"
        };
    }
}
