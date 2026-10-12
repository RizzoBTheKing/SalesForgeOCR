using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.Core.Interfaces
{
    public interface IJobQueue
    {
        Task EnqueueAsync(Func<CancellationToken, Task> job);
        void StartWorker(CancellationToken cancellationToken = default);
    }
}
