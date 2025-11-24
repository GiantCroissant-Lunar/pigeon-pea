using Arch.Core;
using PigeonPea.Game.Contracts.Character.Models;

namespace PigeonPea.Game.Contracts.Character.Services;

public interface IService
{
    CharacterView GetCharacter(World world, Entity entity);
    void SetClass(World world, Entity entity, string classId);
    void AddExperience(World world, Entity entity, int amount);
    void LevelUp(World world, Entity entity);
}
