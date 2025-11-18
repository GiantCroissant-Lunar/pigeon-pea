using ReactiveUI;

namespace PigeonPea.Shared.ViewModels;

public class InventorySlotViewModel : ReactiveObject
{
    private int _slotIndex;
    private string? _definitionId;
    private int _quantity;

    public int SlotIndex
    {
        get => _slotIndex;
        set => this.RaiseAndSetIfChanged(ref _slotIndex, value);
    }

    public string? DefinitionId
    {
        get => _definitionId;
        set => this.RaiseAndSetIfChanged(ref _definitionId, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => this.RaiseAndSetIfChanged(ref _quantity, value);
    }

    public bool IsEmpty => string.IsNullOrEmpty(_definitionId) || _quantity <= 0;
}
