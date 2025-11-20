namespace PigeonPea.Console;

public sealed class DungeonInputState
{
    public bool AttackPressed { get; set; }
    public bool AttackJustPressed { get; set; }

    public bool InteractPressed { get; set; }
    public bool InteractJustPressed { get; set; }

    public bool InventoryPressed { get; set; }
    public bool InventoryJustPressed { get; set; }

    public bool PausePressed { get; set; }
    public bool PauseJustPressed { get; set; }
}
