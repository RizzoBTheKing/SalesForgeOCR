using SalesForgeOCR.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace SalesForgeOCR.Application.Background
{
    public class BackgroundJobQueue : IJobQueue, IDisposable
    {
        private readonly Channel<Func<CancellationToken, Task>> _queue;
        private CancellationTokenSource? _workerCts;

        public BackgroundJobQueue(int capacity = 1000)
        {
            _queue = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false
            });
        }

        public async Task EnqueueAsync(Func<CancellationToken, Task> job)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            await _queue.Writer.WriteAsync(job);
        }

        public void StartWorker(CancellationToken cancellationToken = default)
        {
            if (_workerCts != null) return;
            _workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = Task.Run(async () =>
            {
                while (await _queue.Reader.WaitToReadAsync(_workerCts.Token))
                {
                    while (_queue.Reader.TryRead(out var job))
                    {
                        try
                        {
                            await job(_workerCts.Token);
                        }
                        catch
                        {
                            // swallow - individual jobs should handle errors and logging
                        }
                    }
                }
            }, _workerCts.Token);
        }

        public void Dispose()
        {
            _workerCts?.Cancel();
            _workerCts?.Dispose();
        }
    }
}
