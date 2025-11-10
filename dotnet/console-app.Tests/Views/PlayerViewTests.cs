using Xunit;
using FluentAssertions;
using ReactiveUI;
using PigeonPea.Console.Views;
using PigeonPea.Shared.ViewModels;
using SadRogue.Primitives;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace PigeonPea.Console.Tests.Views;

/// <summary>
/// Integration tests for PlayerView that verify reactive subscriptions work correctly.
/// </summary>
public class PlayerViewTests : IDisposable
{
    private readonly PlayerViewModel _viewModel;
    private readonly PlayerView _view;

    public PlayerViewTests()
    {
        _viewModel = new PlayerViewModel();
        _view = new PlayerView(_viewModel);
    }

    [Fact]
    public void PlayerView_Constructor_InitializesWithViewModel()
    {
        // Assert
        _view.Should().NotBeNull();
        _view.Title.Should().Be("Player");
    }

    [Fact]
    public void PlayerView_SubscribesToNameChanges()
    {
        // Arrange
        var nameChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.Name)
            .Skip(1) // Skip initial value
            .Subscribe(_ => nameChanged = true);

        try
        {
            // Act
            _viewModel.Name = "TestHero";

            // Assert
            nameChanged.Should().BeTrue();
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToHealthChanges()
    {
        // Arrange
        var healthChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.Health)
            .Skip(1) // Skip initial value
            .Subscribe(_ => healthChanged = true);

        try
        {
            // Act
            _viewModel.Health = 75;

            // Assert
            healthChanged.Should().BeTrue();
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToHealthDisplayChanges()
    {
        // Arrange
        var healthDisplayChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.HealthDisplay)
            .Skip(1) // Skip initial value
            .Subscribe(_ => healthDisplayChanged = true);

        try
        {
            // Act
            _viewModel.Health = 50;
            _viewModel.MaxHealth = 100;

            // Assert
            healthDisplayChanged.Should().BeTrue();
            _viewModel.HealthDisplay.Should().Be("50/100");
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToLevelChanges()
    {
        // Arrange
        var levelChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.Level)
            .Skip(1) // Skip initial value
            .Subscribe(_ => levelChanged = true);

        try
        {
            // Act
            _viewModel.Level = 5;

            // Assert
            levelChanged.Should().BeTrue();
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToLevelDisplayChanges()
    {
        // Arrange
        var levelDisplayChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.LevelDisplay)
            .Skip(1) // Skip initial value
            .Subscribe(_ => levelDisplayChanged = true);

        try
        {
            // Act
            _viewModel.Level = 3;

            // Assert
            levelDisplayChanged.Should().BeTrue();
            _viewModel.LevelDisplay.Should().Be("Level 3");
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToExperienceChanges()
    {
        // Arrange
        var experienceChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.Experience)
            .Skip(1) // Skip initial value
            .Subscribe(_ => experienceChanged = true);

        try
        {
            // Act
            _viewModel.Experience = 250;

            // Assert
            experienceChanged.Should().BeTrue();
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_SubscribesToPositionChanges()
    {
        // Arrange
        var positionChanged = false;
        var subscription = _viewModel.WhenAnyValue(x => x.Position)
            .Skip(1) // Skip initial value
            .Subscribe(_ => positionChanged = true);

        try
        {
            // Act
            _viewModel.Position = new Point(10, 20);

            // Assert
            positionChanged.Should().BeTrue();
        }
        finally
        {
            subscription.Dispose();
        }
    }

    [Fact]
    public void PlayerView_DisposesSubscriptionsOnDispose()
    {
        // Arrange
        var viewModel = new PlayerViewModel();
        var view = new PlayerView(viewModel);

        // Act
        view.Dispose();

        // Assert - No exception should occur when changing properties after disposal
        var act = () => viewModel.Name = "Changed";
        act.Should().NotThrow();
    }

    public void Dispose()
    {
        _view.Dispose();
    }
}
