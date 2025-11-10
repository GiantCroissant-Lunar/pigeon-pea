using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// Represents the type of message for color coding.
/// </summary>
public enum MessageType
{
    /// <summary>
    /// Combat-related messages (damage, attacks).
    /// </summary>
    Combat,

    /// <summary>
    /// Inventory-related messages (item pickups, drops).
    /// </summary>
    Inventory,

    /// <summary>
    /// Level and experience messages.
    /// </summary>
    Level,

    /// <summary>
    /// Map and movement messages.
    /// </summary>
    Map,

    /// <summary>
    /// General game state messages.
    /// </summary>
    GameState
}

/// <summary>
/// ViewModel for a single message in the message log.
/// </summary>
public class MessageViewModel : ReactiveObject
{
    private string _text = string.Empty;
    private MessageType _type;
    private DateTime _timestamp;

    /// <summary>
    /// The message text.
    /// </summary>
    public string Text
    {
        get => _text;
        set => this.RaiseAndSetIfChanged(ref _text, value);
    }

    /// <summary>
    /// The type of message for color coding.
    /// </summary>
    public MessageType Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }

    /// <summary>
    /// Timestamp when the message was created.
    /// </summary>
    public DateTime Timestamp
    {
        get => _timestamp;
        set => this.RaiseAndSetIfChanged(ref _timestamp, value);
    }

    /// <summary>
    /// Creates a new MessageViewModel with the specified text and type.
    /// </summary>
    /// <param name="text">The message text.</param>
    /// <param name="type">The message type.</param>
    public MessageViewModel(string text, MessageType type)
    {
        _text = text;
        _type = type;
        _timestamp = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new empty MessageViewModel.
    /// </summary>
    public MessageViewModel()
    {
        _timestamp = DateTime.UtcNow;
    }
}
