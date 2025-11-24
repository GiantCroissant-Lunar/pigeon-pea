using System;

namespace PigeonPea.Game.Contracts.Rendering;

public class HudContext
{
    public RenderContext RenderContext { get; set; } = default!;

    public IServiceProvider Services { get; set; } = default!;
}
