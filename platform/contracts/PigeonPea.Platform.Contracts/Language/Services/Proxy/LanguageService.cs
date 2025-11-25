using System;
using PigeonPea.Platform.Contracts.Language;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Contracts.Core.Attributes;

namespace PigeonPea.Platform.Contracts.Language.Services.Proxy;

/// <summary>
/// Proxy implementation of ILanguageService.
/// </summary>
[RealizeService(typeof(ILanguageService))]
public partial class LanguageService : ILanguageService
{
    private readonly IRegistry _registry;

    public LanguageService(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
