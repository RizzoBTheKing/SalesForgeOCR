using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public abstract class ProcessingStage
    {
        public abstract string Name { get; }
        public abstract Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default);
    }
}
