using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using MessagePipe;
using ObservableCollections;
using PigeonPea.Shared;
using PigeonPea.Shared.Events;
using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for the message log that subscribes to game events and displays messages.
/// </summary>
public class MessageLogViewModel : ReactiveObject, IDisposable
{
    private readonly ObservableList<MessageViewModel> _messages = new();
    private readonly CompositeDisposable _subscriptions = new();
    private const int MaxMessages = 100;

    /// <summary>
    /// Observable collection of messages.
    /// </summary>
    public IObservableCollection<MessageViewModel> Messages => _messages;

    /// <summary>
    /// Creates a new MessageLogViewModel with event subscriptions.
    /// </summary>
    /// <param name="combatSubscriber">Subscriber for combat events.</param>
    /// <param name="inventorySubscriber">Subscriber for inventory events.</param>
    /// <param name="gameStateSubscriber">Subscriber for game state events.</param>
    public MessageLogViewModel(
        ISubscriber<PlayerDamagedEvent> combatSubscriber,
        ISubscriber<ItemPickedUpEvent> inventorySubscriber,
        ISubscriber<GameStateChangedEvent> gameStateSubscriber)
    {
        // Subscribe to combat events
        _subscriptions.Add(combatSubscriber.Subscribe(e =>
        {
            AddMessage(
                $"Took {e.Damage} damage from {e.Source}! ({e.RemainingHealth} HP remaining)",
                MessageType.Combat);
        }));

        // Subscribe to inventory events
        _subscriptions.Add(inventorySubscriber.Subscribe(e =>
        {
            AddMessage(
                $"Picked up {e.ItemName} ({e.ItemType})",
                MessageType.Inventory);
        }));

        // Subscribe to game state events
        _subscriptions.Add(gameStateSubscriber.Subscribe(e =>
        {
            AddMessage(
                $"Game state changed from {e.PreviousState} to {e.NewState}",
                MessageType.GameState);
        }));
    }

    /// <summary>
    /// Adds a message to the log with the specified type.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="type">The message type for color coding.</param>
    public void AddMessage(string text, MessageType type)
    {
        RxApp.MainThreadScheduler.Schedule(() =>
        {
            var message = new MessageViewModel(text, type);
            _messages.Add(message);

            // Keep only the last 100 messages
            while (_messages.Count > MaxMessages)
            {
                _messages.RemoveAt(0);
            }
        });
    }

    /// <summary>
    /// No-op update to satisfy GameViewModel signature while using event-driven updates.
    /// </summary>
    public void Update(GameWorld world)
    {
        // Intentionally left blank; this VM updates via MessagePipe events.
    }

    /// <summary>
    /// Disposes subscriptions.
    /// </summary>
    public void Dispose()
    {
        _subscriptions.Dispose();
        GC.SuppressFinalize(this);
    }
}
