namespace PigeonPea.Platform.Contracts.Hud.Services;

public interface IService
{
    void ShowMessage(string messageId);

    void HideMessage(string messageId);
}
