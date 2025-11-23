using Arch.Core;
using PigeonPea.Game.Contracts.Persistence.Models;

namespace PigeonPea.Game.Contracts.Persistence.Services;

public interface IService
{
    SaveResult SaveWorld(World world, string saveName);
    LoadResult LoadWorld(World world, string saveName);
    SaveResult SaveEntity(World world, Entity entity);
    Entity LoadEntity(World world, string serializedEntity);
}
