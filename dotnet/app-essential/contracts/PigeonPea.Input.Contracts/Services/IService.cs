namespace PigeonPea.Input.Contracts.Services;

public interface IService
{
    bool IsActionPressed(string actionId);

    float GetAxis(string axisId);
}
