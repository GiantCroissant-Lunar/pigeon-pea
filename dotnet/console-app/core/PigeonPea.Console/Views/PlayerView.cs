using System.Reactive.Disposables;
using System.Reactive.Linq;
using PigeonPea.Shared.ViewModels;
using ReactiveUI;
using Terminal.Gui;

namespace PigeonPea.Console.Views;

/// <summary>
/// Terminal.Gui view that displays player information and subscribes to PlayerViewModel changes.
/// </summary>
public class PlayerView : FrameView
{
    private readonly PlayerViewModel _viewModel;
    private readonly Label _nameLabel;
    private readonly Label _healthLabel;
    private readonly Label _levelLabel;
    private readonly Label _experienceLabel;
    private readonly Label _positionLabel;
    private readonly CompositeDisposable _subscriptions;

    /// <summary>
    /// Initializes a new instance of the PlayerView.
    /// </summary>
    /// <param name="viewModel">The PlayerViewModel to bind to.</param>
    public PlayerView(PlayerViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _subscriptions = new CompositeDisposable();

        Title = "Player";
        X = 0;
        Y = 0;
        Width = 30;
        Height = 7;

        // Create labels
        _nameLabel = new Label
        {
            X = 1,
            Y = 0,
            Text = "Name: "
        };

        _healthLabel = new Label
        {
            X = 1,
            Y = 1,
            Text = "Health: "
        };

        _levelLabel = new Label
        {
            X = 1,
            Y = 2,
            Text = "Level: "
        };

        _experienceLabel = new Label
        {
            X = 1,
            Y = 3,
            Text = "XP: "
        };

        _positionLabel = new Label
        {
            X = 1,
            Y = 4,
            Text = "Pos: "
        };

        Add(_nameLabel, _healthLabel, _levelLabel, _experienceLabel, _positionLabel);

        // Subscribe to property changes
        SetupSubscriptions();
    }

    private void SetupSubscriptions()
    {
        // Subscribe to Name changes
        _viewModel.WhenAnyValue(x => x.Name)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(name =>
            {
                _nameLabel.Text = $"Name: {name}";
                SetNeedsDraw();
            })
            .DisposeWith(_subscriptions);

        // Subscribe to HealthDisplay changes
        _viewModel.WhenAnyValue(x => x.HealthDisplay)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(healthDisplay =>
            {
                _healthLabel.Text = $"Health: {healthDisplay}";
                SetNeedsDraw();
            })
            .DisposeWith(_subscriptions);

        // Subscribe to LevelDisplay changes
        _viewModel.WhenAnyValue(x => x.LevelDisplay)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(levelDisplay =>
            {
                _levelLabel.Text = levelDisplay;
                SetNeedsDraw();
            })
            .DisposeWith(_subscriptions);

        // Subscribe to Experience changes
        _viewModel.WhenAnyValue(x => x.Experience)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(experience =>
            {
                _experienceLabel.Text = $"XP: {experience}";
                SetNeedsDraw();
            })
            .DisposeWith(_subscriptions);

        // Subscribe to Position changes
        _viewModel.WhenAnyValue(x => x.Position)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(position =>
            {
                _positionLabel.Text = $"Pos: ({position.X}, {position.Y})";
                SetNeedsDraw();
            })
            .DisposeWith(_subscriptions);
    }

    /// <summary>
    /// Disposes the subscriptions when the view is disposed.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _subscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }
}
