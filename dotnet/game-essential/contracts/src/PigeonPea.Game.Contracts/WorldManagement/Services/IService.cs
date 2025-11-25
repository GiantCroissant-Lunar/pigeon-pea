using Arch.Core;
using PigeonPea.Game.Contracts.WorldManagement.Models;

namespace PigeonPea.Game.Contracts.WorldManagement.Services;

public interface IService
{
    World CreateWorld(string worldId, WorldConfig config);
    void DestroyWorld(string worldId);
    World? GetWorld(string worldId);
    void TransferEntity(Entity entity, World sourceWorld, World targetWorld);
}
