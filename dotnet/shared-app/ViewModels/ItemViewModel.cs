using ReactiveUI;
using PigeonPea.Shared.Components;

namespace PigeonPea.Shared.ViewModels;

/// <summary>
/// ViewModel for an individual item in the inventory.
/// Exposes item state in a format suitable for UI binding.
/// </summary>
public class ItemViewModel : ReactiveObject
{
    private string _name = string.Empty;
    private ItemType _type;

    /// <summary>
    /// The name of the item.
    /// </summary>
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }

    /// <summary>
    /// The type of the item.
    /// </summary>
    public ItemType Type
    {
        get => _type;
        set => this.RaiseAndSetIfChanged(ref _type, value);
    }
}
