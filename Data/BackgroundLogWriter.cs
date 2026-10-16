using System.ComponentModel;
using ElevatorSystem.Models;

namespace ElevatorSystem.Data;

/// <summary>
/// Collects log entries on the UI thread and writes them in batches on a <see cref="BackgroundWorker"/>,
/// so the animation never waits for the database.
/// <para>
/// A WinForms timer flushes the queue every half second. Only one batch is written at a time. If a
/// write fails, the batch goes back to the front of the queue and is retried a few seconds later.
/// </para>
/// </summary>
public sealed class BackgroundLogWriter : IDisposable
{
    private const int FlushIntervalMs = 500;
    private const int RetryDelayMs = 5000;

    // Upper bound so a database that stays unavailable for a long time cannot use unbounded memory.
    private const int MaxPendingEntries = 5000;

    private readonly ElevatorLogRepository _repository;
    private readonly List<LogEntry> _pending = new();
    private readonly List<Action> _afterFlushCallbacks = new();
    private readonly BackgroundWorker _worker = new();
    private readonly System.Windows.Forms.Timer _flushTimer = new() { Interval = FlushIntervalMs };
    private List<LogEntry> _inFlight = new();
    private DateTime _retryAfter = DateTime.MinValue;

    public BackgroundLogWriter(ElevatorLogRepository repository)
    {
        _repository = repository;

        // DoWork runs on a thread-pool thread: it touches only the repository and its own argument.
        _worker.DoWork += (_, e) =>
        {
            var batch = (List<LogEntry>)e.Argument!;
            _repository.InsertLogs(batch);
            e.Result = batch.Count;
        };

        // RunWorkerCompleted runs back on the UI thread, so it can safely raise events that update controls.
        _worker.RunWorkerCompleted += OnWorkerCompleted;
        _flushTimer.Tick += (_, _) => Flush(force: false);
    }

    /// <summary>Raised on the UI thread after a batch is saved; the argument is the number of rows written.</summary>
    public event EventHandler<int>? EntriesWritten;

    /// <summary>Raised on the UI thread when a batch could not be saved.</summary>
    public event EventHandler<DataAccessException>? WriteFailed;

    /// <summary>True while entries are waiting or a batch is being written.</summary>
    public bool HasWork => _pending.Count > 0 || _worker.IsBusy;

    /// <summary>Starts the periodic flush (called once the database has been prepared).</summary>
    public void Start() => _flushTimer.Start();

    public void Enqueue(LogEntry entry)
    {
        _pending.Add(entry);
        if (_pending.Count > MaxPendingEntries)
        {
            _pending.RemoveAt(0);
        }
    }

    /// <summary>
    /// Writes everything that is queued, then runs <paramref name="callback"/> on the UI thread.
    /// Used before showing the log (so it is complete) and before the program exits.
    /// The callback also runs when the write fails, so the caller is never left waiting.
    /// </summary>
    public void FlushThen(Action callback)
    {
        if (!HasWork)
        {
            callback();
            return;
        }

        _afterFlushCallbacks.Add(callback);
        Flush(force: true);
    }

    private void Flush(bool force)
    {
        if (_worker.IsBusy || _pending.Count == 0)
        {
            return;
        }

        // After a failure, wait before retrying unless someone is explicitly waiting for the flush.
        if (!force && DateTime.Now < _retryAfter)
        {
            return;
        }

        _inFlight = new List<LogEntry>(_pending);
        _pending.Clear();
        _worker.RunWorkerAsync(_inFlight);
    }

    private void OnWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        if (e.Error != null)
        {
            // Put the batch back in front of anything queued meanwhile, keeping the original order.
            _pending.InsertRange(0, _inFlight);
            _inFlight = new List<LogEntry>();
            _retryAfter = DateTime.Now.AddMilliseconds(RetryDelayMs);

            DataAccessException error = e.Error as DataAccessException
                ?? new DataAccessException($"Could not save the operation log. {e.Error.Message}", e.Error);
            WriteFailed?.Invoke(this, error);
            RunCallbacks();
            return;
        }

        _inFlight = new List<LogEntry>();
        _retryAfter = DateTime.MinValue;
        EntriesWritten?.Invoke(this, (int)e.Result!);

        // Entries may have arrived while this batch was being written; a waiting caller needs those too.
        if (_afterFlushCallbacks.Count > 0 && _pending.Count > 0)
        {
            Flush(force: true);
        }
        else
        {
            RunCallbacks();
        }
    }

    private void RunCallbacks()
    {
        Action[] callbacks = _afterFlushCallbacks.ToArray();
        _afterFlushCallbacks.Clear();
        foreach (Action callback in callbacks)
        {
            callback();
        }
    }

    public void Dispose()
    {
        _flushTimer.Stop();
        _flushTimer.Dispose();
        _worker.Dispose();
    }
}
