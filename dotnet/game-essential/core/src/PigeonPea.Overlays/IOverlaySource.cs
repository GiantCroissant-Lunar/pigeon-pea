namespace PigeonPea.Overlays;

public interface IOverlaySource<in TContext, TPosition>
{
    IEnumerable<IOverlayFeature<TPosition>> GetOverlays(TContext context);
}
