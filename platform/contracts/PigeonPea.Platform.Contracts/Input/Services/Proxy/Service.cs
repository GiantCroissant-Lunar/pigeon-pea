using PigeonPea.Platform.Contracts.Core;

namespace PigeonPea.Platform.Contracts.Input.Services.Proxy;

public class Service : IService
{
    private readonly IRegistry _registry;

    public Service(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    private IService ResolveImplementation()
    {
        return _registry.Get<IService>();
    }

    public void Update()
        => ResolveImplementation().Update();

    public bool IsActionPressed(string actionName)
        => ResolveImplementation().IsActionPressed(actionName);

    public bool IsActionJustPressed(string actionName)
        => ResolveImplementation().IsActionJustPressed(actionName);

    public bool IsActionJustReleased(string actionName)
        => ResolveImplementation().IsActionJustReleased(actionName);

    public float GetActionStrength(string actionName)
        => ResolveImplementation().GetActionStrength(actionName);
}
