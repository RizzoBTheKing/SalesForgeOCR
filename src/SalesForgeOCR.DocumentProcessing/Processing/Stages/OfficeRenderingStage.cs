using SalesForgeOCR.DocumentProcessing.Engines;
namespace SalesForgeOCR.DocumentProcessing.Processing.Stages;
public class OfficeRenderingStage : ProcessingStage
{
    public override string Name=>"Office template rendering";
    public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context,CancellationToken cancellationToken=default)
    {
        var path=context.Version.StoragePath??context.Version.FileName;
        if(Path.GetExtension(path).ToLowerInvariant() is ".docx" or ".doc" or ".xlsx")context.RenderedPdfPath=await OfficePdfRenderer.RenderAsync(path,cancellationToken);
        return ProcessingResult.Ok();
    }
}
