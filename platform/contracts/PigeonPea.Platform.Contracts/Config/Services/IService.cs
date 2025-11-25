namespace PigeonPea.Platform.Contracts.Config.Services;

public interface IService
{
    string? GetValue(string key);

    T? GetValue<T>(string key);

    bool TryGetValue(string key, out string value);
}
