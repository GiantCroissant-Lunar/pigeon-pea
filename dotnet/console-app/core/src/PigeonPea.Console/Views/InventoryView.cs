using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using ObservableCollections;
using PigeonPea.Shared.ViewModels;
using ReactiveUI;
using Terminal.Gui;

namespace PigeonPea.Console.Views;

/// <summary>
/// Terminal.Gui view that displays inventory and subscribes to InventoryViewModel changes.
/// </summary>
public class InventoryView : FrameView
{
    private readonly InventoryViewModel _viewModel;
    private readonly ListView _listView;
    private readonly CompositeDisposable _subscriptions;
    private List<string> _itemDisplayList;

    /// <summary>
    /// Initializes a new instance of the InventoryView.
    /// </summary>
    /// <param name="viewModel">The InventoryViewModel to bind to.</param>
    public InventoryView(InventoryViewModel viewModel)
    {
        _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _subscriptions = new CompositeDisposable();
        _itemDisplayList = new List<string>();

        Title = "Inventory";
        X = 0;
        Y = 7;
        Width = 30;
        Height = 10;

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

        // Subscribe to collection changes and selected item changes
        SetupSubscriptions();

        // Initial update
        UpdateListView();
    }

    private void OnItemsCollectionChanged(in NotifyCollectionChangedEventArgs<ItemViewModel> args)
    {
        // ReactiveUI already marshals to the main thread scheduler, so we can call directly
        UpdateListView();
    }

    private void OnListViewSelectedItemChanged(object? sender, ListViewItemEventArgs args)
    {
        _viewModel.SelectedIndex = _listView.SelectedItem;
    }

    private void SetupSubscriptions()
    {
        // Subscribe to collection changes using CollectionChanged event
        _viewModel.Items.CollectionChanged += OnItemsCollectionChanged;

        // Subscribe to SelectedIndex changes
        _viewModel.WhenAnyValue(x => x.SelectedIndex)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(index =>
            {
                if (index >= 0 && index < _itemDisplayList.Count)
                {
                    _listView.SelectedItem = index;
                }
            })
            .DisposeWith(_subscriptions);

        // Update ViewModel when list view selection changes
        _listView.SelectedItemChanged += OnListViewSelectedItemChanged;
    }

    private void UpdateListView()
    {
        // Build display list from view model items
        _itemDisplayList = _viewModel.Items
            .Select(item => $"{item.Name} ({item.Type})")
            .ToList();

        if (_itemDisplayList.Count == 0)
        {
            _itemDisplayList.Add("(empty)");
        }

        // Update the list view source
        _listView.SetSource(new ObservableCollection<string>(_itemDisplayList));

        SetNeedsDraw();
    }

    /// <summary>
    /// Disposes the subscriptions when the view is disposed.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _viewModel.Items.CollectionChanged -= OnItemsCollectionChanged;
            _listView.SelectedItemChanged -= OnListViewSelectedItemChanged;
            _subscriptions?.Dispose();
        }
        base.Dispose(disposing);
    }
}
