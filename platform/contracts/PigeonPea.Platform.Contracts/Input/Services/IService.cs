namespace PigeonPea.Platform.Contracts.Input.Services;

public interface IService
{
    void Update();
    bool IsActionPressed(string actionName);
    bool IsActionJustPressed(string actionName);
    bool IsActionJustReleased(string actionName);
    float GetActionStrength(string actionName);
}
