using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Arch.Core;
using Arch.Core.Extensions;
using PigeonPea.Contracts.Plugin;
using PigeonPea.Game.Contracts.Avatar.Models;
using PigeonPea.Game.Contracts.Avatar.Services;
using PigeonPea.Shared.ECS.Components;

namespace PigeonPea.Plugins.Avatar.Basic;

public class BasicAvatarService : IService, IPlugin
{
    public string Id => "pigeon-pea.plugins.avatar.basic";
    public string Name => "Basic Avatar Service";
    public string Version => "0.1.0";

    public Task InitializeAsync(IPluginContext context, CancellationToken ct = default)
    {
        context.Registry.Register<IService>(this);
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task StopAsync(CancellationToken ct = default) => Task.CompletedTask;

    public AvatarView GetAvatar(World world, Entity entity)
    {
        var view = new AvatarView();

        if (entity.Has<PigeonPea.Shared.ECS.Components.Avatar>())
        {
            var avatar = entity.Get<PigeonPea.Shared.ECS.Components.Avatar>();
            view.Appearance = new AppearanceData
            {
                BodyType = avatar.BodyType,
                Features = new Dictionary<string, string>(avatar.Features ?? new Dictionary<string, string>()),
                Colors = new Dictionary<string, string>(avatar.Colors ?? new Dictionary<string, string>())
            };
        }

        if (entity.Has<AvatarDisplay>())
        {
            var display = entity.Get<AvatarDisplay>();
            view.DisplayName = display.DisplayName;
            view.Title = display.Title;
        }

        if (entity.Has<CosmeticEquipment>())
        {
            var equipment = entity.Get<CosmeticEquipment>();
            view.CosmeticEquipment = new Dictionary<string, string>(equipment.Slots ?? new Dictionary<string, string>());
        }

        return view;
    }

    public void SetAppearance(World world, Entity entity, AppearanceData appearance)
    {
        if (!entity.Has<PigeonPea.Shared.ECS.Components.Avatar>())
        {
            entity.Add(new PigeonPea.Shared.ECS.Components.Avatar());
        }

        ref var avatar = ref entity.Get<PigeonPea.Shared.ECS.Components.Avatar>();
        avatar.BodyType = appearance.BodyType;
        avatar.Features = new Dictionary<string, string>(appearance.Features);
        avatar.Colors = new Dictionary<string, string>(appearance.Colors);
    }

    public void EquipCosmetic(World world, Entity entity, string slot, string itemId)
    {
        if (!entity.Has<CosmeticEquipment>())
        {
            entity.Add(new CosmeticEquipment { Slots = new Dictionary<string, string>() });
        }

        ref var equipment = ref entity.Get<CosmeticEquipment>();
        if (equipment.Slots == null) equipment.Slots = new Dictionary<string, string>();

        equipment.Slots[slot] = itemId;
    }

    public void SetDisplayInfo(World world, Entity entity, string displayName, string title)
    {
        if (!entity.Has<AvatarDisplay>())
        {
            entity.Add(new AvatarDisplay());
        }

        ref var display = ref entity.Get<AvatarDisplay>();
        display.DisplayName = displayName;
        display.Title = title;
    }
}

