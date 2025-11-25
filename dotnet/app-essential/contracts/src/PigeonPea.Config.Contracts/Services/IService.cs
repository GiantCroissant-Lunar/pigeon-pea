namespace PigeonPea.Config.Contracts.Services;

public interface IService
{
    string? GetValue(string key);

    T? GetValue<T>(string key);

    bool TryGetValue(string key, out string value);
}
