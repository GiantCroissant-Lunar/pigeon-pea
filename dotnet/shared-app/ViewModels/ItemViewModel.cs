using Arch.Core;
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
    /// Reference to the source Entity in the ECS world.
    /// Used for efficient synchronization and uniquely identifying items.
    /// </summary>
    public required Entity SourceEntity { get; init; }

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
