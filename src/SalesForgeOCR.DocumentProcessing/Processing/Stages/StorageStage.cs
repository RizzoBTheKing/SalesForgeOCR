using SalesForgeOCR.Core.Interfaces;
namespace SalesForgeOCR.DocumentProcessing.Processing.Stages;
public class StorageStage : ProcessingStage
{
    private readonly IDocumentArtifactStore _store;
    public StorageStage(IDocumentArtifactStore store) => _store = store;
    public override string Name => "Storage";
    public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
    {
        await _store.SaveAsync(context.Document.Id, context.Version.Id, context.ExtractedText, context.Entities, context.Metadata, context.Images, context.Tables, cancellationToken);
        return ProcessingResult.Ok();
    }
}
