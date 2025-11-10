using System.Collections.ObjectModel;
using ObservableCollections;
using Terminal.Gui;
using PigeonPea.Shared.ViewModels;

namespace PigeonPea.Console.Views;

/// <summary>
/// Terminal.Gui view that displays game messages and subscribes to MessageLogViewModel changes.
/// </summary>
public class MessageLogView : FrameView
{
    private readonly MessageLogViewModel _viewModel;
    private readonly ListView _listView;
    private List<string> _messageDisplayList;

    /// <summary>
    /// Initializes a new instance of the MessageLogView.
    /// </summary>
    /// <param name="viewModel">The MessageLogViewModel to bind to.</param>
    public MessageLogView(MessageLogViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _messageDisplayList = new List<string>();

        Title = "Messages";
        X = 30;
        Y = 0;
        Width = Dim.Fill();
        Height = Dim.Fill() - 3;

        // Create list view
        _listView = new ListView
        {
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            Height = Dim.Fill(),
            AllowsMarking = false
        };

        Add(_listView);

        // Subscribe to collection changes
        SetupSubscriptions();

        // Initial update
        UpdateListView();
    }

    private void OnMessagesCollectionChanged(in NotifyCollectionChangedEventArgs<MessageViewModel> args)
    {
        UpdateListView();
        // Auto-scroll to bottom when new message arrives
        if (_messageDisplayList.Count > 1) // More than just "(no messages)"
        {
            _listView.SelectedItem = _messageDisplayList.Count - 1;
        }
    }

    private void SetupSubscriptions()
    {
        // Subscribe to collection changes using CollectionChanged event
        // ObservableCollections uses a different event signature (ref parameter)
        ((ObservableList<MessageViewModel>)_viewModel.Messages).CollectionChanged += OnMessagesCollectionChanged;
    }

    private void UpdateListView()
    {
        // Build display list from view model messages
        _messageDisplayList = _viewModel.Messages
            .Select(msg => FormatMessage(msg))
            .ToList();

        if (_messageDisplayList.Count == 0)
        {
            _messageDisplayList.Add("(no messages)");
        }

        // Update the list view source
        _listView.SetSource(new ObservableCollection<string>(_messageDisplayList));
        
        SetNeedsDraw();
    }

    private string FormatMessage(MessageViewModel message)
    {
        // Format message with timestamp and color indicator
        var typeIndicator = message.Type switch
        {
            MessageType.Combat => "[!]",
            MessageType.Inventory => "[+]",
            MessageType.Level => "[*]",
            MessageType.Map => "[~]",
            MessageType.GameState => "[#]",
            _ => "[ ]"
        };

        return $"{typeIndicator} {message.Text}";
    }

    /// <summary>
    /// Disposes the subscriptions when the view is disposed.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ((ObservableList<MessageViewModel>)_viewModel.Messages).CollectionChanged -= OnMessagesCollectionChanged;
        }
        base.Dispose(disposing);
    }
}
