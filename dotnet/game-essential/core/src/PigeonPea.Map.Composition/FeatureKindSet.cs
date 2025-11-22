using PigeonPea.Map.Contracts.Features;

namespace PigeonPea.Map.Composition;

public record FeatureKindSet(params FeatureKind[] Kinds)
{
    public bool Contains(FeatureKind kind) => Kinds.Contains(kind);
}
