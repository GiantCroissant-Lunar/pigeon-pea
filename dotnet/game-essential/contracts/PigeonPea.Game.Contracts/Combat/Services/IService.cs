using Arch.Core;

namespace PigeonPea.Game.Contracts.Combat.Services;

public interface IService
{
    int CalculateMeleeDamage(World world, Entity attacker, Entity defender);
}
