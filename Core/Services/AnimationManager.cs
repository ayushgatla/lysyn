using System;
using System.IO;
using System.Windows.Threading;
using LyricIsland.Core.Models;

namespace LyricIsland.Core.Services;

public class AnimationManager : IDisposable
{
    private readonly DispatcherTimer _songChangeTimer;
    private AppSettings _settings;
    private PlaybackState _currentState = PlaybackState.Stopped;
    private bool _inSongChangeTransition;

    public string CurrentLeftGif { get; private set; } = string.Empty;
    public string CurrentRightGif { get; private set; } = string.Empty;
    public bool LeftFlip => _settings.LeftCharacter.FlipHorizontal;
    public bool RightFlip => _settings.RightCharacter.FlipHorizontal;

    public event Action? CharactersUpdated;

    public AnimationManager(AppSettings settings)
    {
        _settings = settings;
        _songChangeTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2.5)
        };
        _songChangeTimer.Tick += OnSongChangeTimerTick;

        UpdateCharacters();
    }

    public void UpdateSettings(AppSettings settings)
    {
        _settings = settings;
        UpdateCharacters();
    }

    public void OnPlaybackStateChanged(PlaybackState state)
    {
        _currentState = state;
        if (!_inSongChangeTransition)
        {
            UpdateCharacters();
        }
    }

    public void OnSongChanged()
    {
        var hasSongChangeGif = !string.IsNullOrWhiteSpace(_settings.LeftCharacter.SongChangedGifPath) ||
                               !string.IsNullOrWhiteSpace(_settings.RightCharacter.SongChangedGifPath);

        if (hasSongChangeGif && _currentState == PlaybackState.Playing)
        {
            _inSongChangeTransition = true;
            _songChangeTimer.Stop();
            _songChangeTimer.Start();
        }

        UpdateCharacters();
    }

    private void OnSongChangeTimerTick(object? sender, EventArgs e)
    {
        _songChangeTimer.Stop();
        _inSongChangeTransition = false;
        UpdateCharacters();
    }

    private void UpdateCharacters()
    {
        CurrentLeftGif = ResolveGifPath(_settings.LeftCharacter);
        CurrentRightGif = ResolveGifPath(_settings.RightCharacter);
        CharactersUpdated?.Invoke();
    }

    private string ResolveGifPath(CharacterConfig config)
    {
        if (!config.Enabled) return string.Empty;

        if (_inSongChangeTransition && !string.IsNullOrWhiteSpace(config.SongChangedGifPath) && File.Exists(config.SongChangedGifPath))
        {
            return config.SongChangedGifPath;
        }

        string path = _currentState switch
        {
            PlaybackState.Playing => !string.IsNullOrWhiteSpace(config.PlayingGifPath) ? config.PlayingGifPath : config.IdleGifPath,
            PlaybackState.Paused => !string.IsNullOrWhiteSpace(config.PausedGifPath) ? config.PausedGifPath : config.IdleGifPath,
            _ => config.IdleGifPath
        };

        return path;
    }

    public void Dispose()
    {
        _songChangeTimer.Stop();
    }
}
