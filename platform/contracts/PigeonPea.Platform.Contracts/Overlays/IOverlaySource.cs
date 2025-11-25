using System.Collections.Generic;

namespace PigeonPea.Platform.Contracts.Overlays;

public interface IOverlaySource<in TContext, TPosition>
{
    IEnumerable<IOverlayFeature<TPosition>> GetOverlays(TContext context);
}
