using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public class DocumentProcessingJob
    {
        private readonly ProcessingPipeline _pipeline;
        private readonly IDocumentRepository _repo;

        public DocumentProcessingJob(ProcessingPipeline pipeline, IDocumentRepository repo)
        {
            _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        public async Task<ProcessingResult> RunAsync(Document doc, DocumentVersion version, CancellationToken cancellationToken = default)
        {
            var context = new ProcessingContext(doc, version);
            var result = await _pipeline.ExecuteAsync(context, cancellationToken);

            // In a real implementation we would persist extracted artifacts
            return result;
        }
    }
}
