using System;
using PigeonPea.Platform.Contracts.Time;
using PigeonPea.Platform.Contracts.Core;
using PigeonPea.Platform.Contracts.Core.Attributes;

namespace PigeonPea.Platform.Contracts.Time.Services.Proxy;

/// <summary>
/// Proxy implementation of ICalendarService.
/// </summary>
[RealizeService(typeof(ICalendarService))]
public partial class CalendarService : ICalendarService
{
    private readonly IRegistry _registry;

    public CalendarService(IRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }
}
