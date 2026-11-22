using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;
using LyricIsland.Core.Models;

namespace LyricIsland.Core.Services;

public class MediaSessionManager : IDisposable
{
    private GlobalSystemMediaTransportControlsSessionManager? _sessionManager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly object _lock = new();

    public event Action<MediaTrackInfo>? TrackChanged;
    public event Action<PlaybackState>? PlaybackStateChanged;
    public event Action<TimeSpan>? TimelinePositionChanged;

    public MediaTrackInfo CurrentTrack { get; private set; } = new();

    public async Task InitializeAsync()
    {
        try
        {
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged += OnCurrentSessionChanged;
                UpdateCurrentSession(_sessionManager.GetCurrentSession());
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize SMTC SessionManager: {ex.Message}");
        }
    }

    private void OnCurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
    {
        UpdateCurrentSession(sender.GetCurrentSession());
    }

    private void UpdateCurrentSession(GlobalSystemMediaTransportControlsSession? newSession)
    {
        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }

            _currentSession = newSession;

            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
            }
        }

        _ = RefreshSessionDetailsAsync();
    }

    private void OnMediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
    {
        _ = RefreshSessionDetailsAsync();
    }

    private void OnPlaybackInfoChanged(GlobalSystemMediaTransportControlsSession sender, PlaybackInfoChangedEventArgs args)
    {
        RefreshPlaybackInfo(sender);
    }

    private void OnTimelinePropertiesChanged(GlobalSystemMediaTransportControlsSession sender, TimelinePropertiesChangedEventArgs args)
    {
        try
        {
            var timeline = sender.GetTimelineProperties();
            var position = timeline.Position;
            CurrentTrack.Position = position;
            TimelinePositionChanged?.Invoke(position);
        }
        catch { }
    }

    public async Task RefreshSessionDetailsAsync()
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_lock)
        {
            session = _currentSession;
        }

        if (session == null)
        {
            CurrentTrack = new MediaTrackInfo { State = PlaybackState.Stopped };
            TrackChanged?.Invoke(CurrentTrack);
            PlaybackStateChanged?.Invoke(PlaybackState.Stopped);
            return;
        }

        try
        {
            var mediaProps = await session.TryGetMediaPropertiesAsync();
            var playbackInfo = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            var track = new MediaTrackInfo
            {
                Title = mediaProps?.Title ?? string.Empty,
                Artist = mediaProps?.Artist ?? string.Empty,
                Album = mediaProps?.AlbumTitle ?? string.Empty,
                SourceApp = session.SourceAppUserModelId ?? string.Empty,
                Duration = timeline?.EndTime ?? TimeSpan.Zero,
                Position = timeline?.Position ?? TimeSpan.Zero,
                State = ConvertPlaybackStatus(playbackInfo?.PlaybackStatus)
            };

            if (mediaProps?.Thumbnail != null)
            {
                track.ThumbnailBytes = await ReadStreamBytesAsync(mediaProps.Thumbnail);
            }

            CurrentTrack = track;
            TrackChanged?.Invoke(track);
            PlaybackStateChanged?.Invoke(track.State);
            TimelinePositionChanged?.Invoke(track.Position);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error reading SMTC details: {ex.Message}");
        }
    }

    private void RefreshPlaybackInfo(GlobalSystemMediaTransportControlsSession session)
    {
        try
        {
            var playbackInfo = session.GetPlaybackInfo();
            var state = ConvertPlaybackStatus(playbackInfo?.PlaybackStatus);
            CurrentTrack.State = state;
            PlaybackStateChanged?.Invoke(state);
        }
        catch { }
    }

    private static PlaybackState ConvertPlaybackStatus(GlobalSystemMediaTransportControlsSessionPlaybackStatus? status)
    {
        return status switch
        {
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing => PlaybackState.Playing,
            GlobalSystemMediaTransportControlsSessionPlaybackStatus.Paused => PlaybackState.Paused,
            _ => PlaybackState.Stopped
        };
    }

    private static async Task<byte[]?> ReadStreamBytesAsync(IRandomAccessStreamReference streamRef)
    {
        try
        {
            using var stream = await streamRef.OpenReadAsync();
            using var ms = new MemoryStream();
            using var netStream = stream.AsStreamForRead();
            await netStream.CopyToAsync(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> TogglePlayPauseAsync()
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_lock) { session = _currentSession; }
        if (session == null) return false;
        try { return await session.TryTogglePlayPauseAsync(); }
        catch { return false; }
    }

    public async Task<bool> SkipPreviousAsync()
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_lock) { session = _currentSession; }
        if (session == null) return false;
        try { return await session.TrySkipPreviousAsync(); }
        catch { return false; }
    }

    public async Task<bool> SkipNextAsync()
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_lock) { session = _currentSession; }
        if (session == null) return false;
        try { return await session.TrySkipNextAsync(); }
        catch { return false; }
    }

    public async Task<bool> ChangePlaybackPositionAsync(TimeSpan position)
    {
        GlobalSystemMediaTransportControlsSession? session;
        lock (_lock) { session = _currentSession; }
        if (session == null) return false;
        try { return await session.TryChangePlaybackPositionAsync(position.Ticks); }
        catch { return false; }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_currentSession != null)
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
                _currentSession = null;
            }

            if (_sessionManager != null)
            {
                _sessionManager.CurrentSessionChanged -= OnCurrentSessionChanged;
                _sessionManager = null;
            }
        }
    }
}
