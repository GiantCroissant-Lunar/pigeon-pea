namespace PigeonPea.Platform.Contracts.Rendering.Services;

public interface IService
{
    void BeginFrame();
    void EndFrame();
    void Draw(IRenderTarget target, Viewport viewport);
    void Clear();
}
