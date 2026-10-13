using System.Threading.Channels;
using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.Application.Background;
public class BackgroundJobQueue : IJobQueue, IDisposable
{
    private readonly Channel<Func<CancellationToken, Task>> _queue;
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _worker;
    private bool _disposed;
    public BackgroundJobQueue(int capacity = 1000) => _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(capacity) { SingleReader = true });
    public async Task EnqueueAsync(Func<CancellationToken, Task> job)
    {
        ArgumentNullException.ThrowIfNull(job);
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _queue.Writer.WriteAsync(job);
    }
    public void StartWorker(CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            if (_worker != null) return;
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var token = _cts.Token;
            _worker = Task.Run(async () =>
            {
                try
                {
                    await foreach (var job in _queue.Reader.ReadAllAsync(token))
                    {
                        try { await job(token); }
                        catch (OperationCanceledException) when (token.IsCancellationRequested) { break; }
                        catch (Exception error) { System.Diagnostics.Trace.TraceError(error.ToString()); }
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            });
        }
    }
    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            _queue.Writer.TryComplete();
            _cts?.Cancel();
        }
        // Worker runs entirely in the background, so waiting cannot block a UI continuation.
        _worker?.GetAwaiter().GetResult();
        _cts?.Dispose();
    }
}
