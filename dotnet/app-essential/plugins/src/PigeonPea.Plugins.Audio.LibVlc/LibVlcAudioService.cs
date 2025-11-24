using System;
using System.Collections.Concurrent;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using PigeonPea.Audio.Contracts;

namespace PigeonPea.Plugins.Audio.LibVlc;

public sealed class LibVlcAudioService : IService, IDisposable
{
    private readonly ILogger _logger;
    private readonly LibVLC _libVlc;
    private readonly ConcurrentDictionary<string, MediaPlayerEntry> _players = new();
    private float _masterVolume = 1.0f;

    private sealed class MediaPlayerEntry : IDisposable
    {
        public MediaPlayerEntry(MediaPlayer player, Media media, float volume, bool loop)
        {
            Player = player;
            Media = media;
            Volume = volume;
            Loop = loop;
        }

        public MediaPlayer Player { get; }
        public Media Media { get; }
        public float Volume { get; set; }
        public bool Loop { get; set; }

        public void Dispose()
        {
            Player.Dispose();
            Media.Dispose();
        }
    }

    public LibVlcAudioService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Core.Initialize();
        _libVlc = new LibVLC();
    }

    public void Play(string soundId, float volume = 1.0f, float pitch = 1.0f, bool loop = false)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            throw new ArgumentException("Sound id must not be null or whitespace.", nameof(soundId));
        }

        var entry = _players.AddOrUpdate(
            soundId,
            id => CreateEntry(id, volume, loop),
            (id, existing) =>
            {
                existing.Volume = volume;
                existing.Loop = loop;
                return existing;
            });

        ApplyVolume(entry);

        if (entry.Player.State == State.Playing)
        {
            entry.Player.Stop();
        }

        entry.Player.Play();
    }

    public void Stop(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            throw new ArgumentException("Sound id must not be null or whitespace.", nameof(soundId));
        }

        if (_players.TryGetValue(soundId, out var entry))
        {
            entry.Player.Stop();
        }
    }

    public void StopAll()
    {
        foreach (var entry in _players.Values)
        {
            entry.Player.Stop();
        }
    }

    public bool IsPlaying(string soundId)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            return false;
        }

        if (!_players.TryGetValue(soundId, out var entry))
        {
            return false;
        }

        return entry.Player.State is State.Playing or State.Opening or State.Buffering;
    }

    public void SetVolume(string soundId, float volume)
    {
        if (string.IsNullOrWhiteSpace(soundId))
        {
            throw new ArgumentException("Sound id must not be null or whitespace.", nameof(soundId));
        }

        if (!_players.TryGetValue(soundId, out var entry))
        {
            return;
        }

        entry.Volume = volume;
        ApplyVolume(entry);
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Math.Clamp(volume, 0.0f, 1.0f);

        foreach (var entry in _players.Values)
        {
            ApplyVolume(entry);
        }
    }

    private MediaPlayerEntry CreateEntry(string soundId, float volume, bool loop)
    {
        var media = CreateMedia(soundId);
        var player = new MediaPlayer(media);

        var entry = new MediaPlayerEntry(player, media, volume, loop);

        player.EndReached += (_, _) =>
        {
            if (entry.Loop)
            {
                player.Stop();
                player.Play();
            }
        };

        return entry;
    }

    private Media CreateMedia(string soundId)
    {
        if (Uri.TryCreate(soundId, UriKind.Absolute, out var uri))
        {
            return new Media(_libVlc, uri);
        }

        return new Media(_libVlc, soundId, FromType.FromPath);
    }

    private void ApplyVolume(MediaPlayerEntry entry)
    {
        var clamped = Math.Clamp(entry.Volume, 0.0f, 1.0f) * Math.Clamp(_masterVolume, 0.0f, 1.0f);
        var scaled = (int)(clamped * 100.0f);
        entry.Player.Volume = scaled;
    }

    public void Dispose()
    {
        foreach (var entry in _players.Values)
        {
            entry.Dispose();
        }

        _players.Clear();
        _libVlc.Dispose();
    }
}
