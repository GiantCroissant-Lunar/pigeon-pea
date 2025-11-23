using System.Threading.Tasks;
using Arch.Core;
using PigeonPea.Game.Contracts.Scenes.Models;

namespace PigeonPea.Game.Contracts.Scenes.Services;

public interface IService
{
    Task InitializeDungeonAsync(World world, DungeonBootstrapOptions options);
}
