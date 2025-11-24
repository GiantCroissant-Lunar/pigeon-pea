using System.Collections.Generic;

namespace PigeonPea.Shared.ECS.Components;

public struct Avatar
{
    public string BodyType;
    public Dictionary<string, string> Features;
    public Dictionary<string, string> Colors;
}
