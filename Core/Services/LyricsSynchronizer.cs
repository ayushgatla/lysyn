using System;
using System.Diagnostics;
using System.Windows.Threading;
using LyricIsland.Core.Models;

namespace LyricIsland.Core.Services;

public class LyricSyncEventArgs : EventArgs
{
    public int ActiveIndex { get; set; } = -1;
    public LyricLine? PreviousLine { get; set; }
    public LyricLine? CurrentLine { get; set; }
    public LyricLine? NextLine { get; set; }
    public double LineProgressRatio { get; set; }
    public TimeSpan CurrentPosition { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public bool HasSyncedLyrics { get; set; }
    public string FallbackText { get; set; } = string.Empty;
}

public class LyricsSynchronizer : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Stopwatch _stopwatch = new();
    private TimeSpan _basePosition = TimeSpan.Zero;
    private LyricData? _currentLyrics;

    public int TimingOffsetMs { get; set; } = 0;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public bool IsPlaying { get; private set; }

    public event EventHandler<LyricSyncEventArgs>? Synchronized;

    public LyricsSynchronizer()
    {
        // 30-40 updates per second for smooth text & progress transitions during playback
        _timer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(25)
        };
        _timer.Tick += OnTimerTick;
    }

    public void SetLyrics(LyricData? lyrics, TimeSpan duration)
    {
        _currentLyrics = lyrics;
        Duration = duration;
        TriggerImmediateSync();
    }

    public void UpdatePlaybackState(PlaybackState state, TimeSpan position)
    {
        _basePosition = position;
        _stopwatch.Restart();

        if (state == PlaybackState.Playing)
        {
            IsPlaying = true;
            if (!_timer.IsEnabled)
            {
                _timer.Start();
            }
        }
        else
        {
            IsPlaying = false;
            _stopwatch.Stop();
            _timer.Stop();
        }

        TriggerImmediateSync();
    }

    public void SyncTimelinePosition(TimeSpan position)
    {
        _basePosition = position;
        if (IsPlaying)
        {
            _stopwatch.Restart();
        }
        TriggerImmediateSync();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        TriggerImmediateSync();
    }

    private void TriggerImmediateSync()
    {
        var currentPosition = _basePosition;
        if (IsPlaying)
        {
            currentPosition += _stopwatch.Elapsed;
        }

        // Apply manual timing offset
        var adjustedPosition = currentPosition + TimeSpan.FromMilliseconds(TimingOffsetMs);
        if (adjustedPosition < TimeSpan.Zero) adjustedPosition = TimeSpan.Zero;

        var args = CalculateSyncArgs(adjustedPosition, currentPosition);
        Synchronized?.Invoke(this, args);
    }

    private LyricSyncEventArgs CalculateSyncArgs(TimeSpan adjustedPosition, TimeSpan rawPosition)
    {
        var args = new LyricSyncEventArgs
        {
            CurrentPosition = rawPosition,
            TotalDuration = Duration,
            HasSyncedLyrics = _currentLyrics?.HasSyncedLyrics == true
        };

        if (_currentLyrics == null)
        {
            args.FallbackText = "Waiting for media...";
            return args;
        }

        if (!_currentLyrics.HasSyncedLyrics)
        {
            if (_currentLyrics.Instrumental)
            {
                args.FallbackText = "♪ Instrumental ♪";
            }
            else if (!string.IsNullOrWhiteSpace(_currentLyrics.PlainLyrics))
            {
                args.FallbackText = "Plain lyrics available (Unsynced)";
            }
            else
            {
                args.FallbackText = "No lyrics found for this track";
            }
            return args;
        }

        var lines = _currentLyrics.ParsedLines;
        int activeIdx = -1;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Timestamp <= adjustedPosition)
            {
                activeIdx = i;
            }
            else
            {
                break;
            }
        }

        args.ActiveIndex = activeIdx;

        if (activeIdx >= 0 && activeIdx < lines.Count)
        {
            args.CurrentLine = lines[activeIdx];
            if (activeIdx > 0)
            {
                args.PreviousLine = lines[activeIdx - 1];
            }
            if (activeIdx + 1 < lines.Count)
            {
                args.NextLine = lines[activeIdx + 1];

                // Calculate progress within this line
                var lineStart = lines[activeIdx].Timestamp;
                var lineEnd = lines[activeIdx + 1].Timestamp;
                var lineDuration = (lineEnd - lineStart).TotalMilliseconds;
                if (lineDuration > 0)
                {
                    var elapsedInLine = (adjustedPosition - lineStart).TotalMilliseconds;
                    args.LineProgressRatio = Math.Clamp(elapsedInLine / lineDuration, 0.0, 1.0);
                }
            }
            else
            {
                args.LineProgressRatio = 1.0;
            }
        }
        else if (lines.Count > 0)
        {
            // Before the first line begins
            args.NextLine = lines[0];
            args.CurrentLine = new LyricLine(TimeSpan.Zero, "•••");
            args.LineProgressRatio = 0.0;
        }

        return args;
    }

    public void Dispose()
    {
        _timer.Stop();
        _stopwatch.Stop();
    }
}
