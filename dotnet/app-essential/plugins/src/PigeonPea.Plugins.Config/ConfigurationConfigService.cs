using System;
using Microsoft.Extensions.Configuration;
using PigeonPea.Config.Contracts;

namespace PigeonPea.Plugins.Config;

public class ConfigurationConfigService : IService
{
    private readonly IConfiguration _configuration;

    public ConfigurationConfigService(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public string? GetValue(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        }

        return _configuration[key];
    }

    public T? GetValue<T>(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        }

        return _configuration.GetValue<T?>(key);
    }

    public bool TryGetValue(string key, out string value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        }

        var result = _configuration[key];
        if (result is null)
        {
            value = string.Empty;
            return false;
        }

        value = result;
        return true;
    }
}
