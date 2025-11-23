using Arch.Core;
using PigeonPea.Game.Contracts.Avatar.Models;

namespace PigeonPea.Game.Contracts.Avatar.Services;

public interface IService
{
    AvatarView GetAvatar(World world, Entity entity);
    void SetAppearance(World world, Entity entity, AppearanceData appearance);
    void EquipCosmetic(World world, Entity entity, string slot, string itemId);
    void SetDisplayInfo(World world, Entity entity, string displayName, string title);
}

