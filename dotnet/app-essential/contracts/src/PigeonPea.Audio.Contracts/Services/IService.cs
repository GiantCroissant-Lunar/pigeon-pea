namespace PigeonPea.Audio.Contracts.Services;

public interface IService
{
    void Play(string soundId, float volume = 1.0f, float pitch = 1.0f, bool loop = false);

    void Stop(string soundId);

    void StopAll();

    bool IsPlaying(string soundId);

    void SetVolume(string soundId, float volume);

    void SetMasterVolume(float volume);
}
