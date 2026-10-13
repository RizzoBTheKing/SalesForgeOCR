using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class TableExtractionStage : ProcessingStage
    {
        private readonly ITableExtractor _tableExtractor;
        public TableExtractionStage(ITableExtractor tableExtractor)
        {
            _tableExtractor = tableExtractor;
        }

        public override string Name => "TableExtraction";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            var tables = await _tableExtractor.ExtractTablesAsync(context.Version.StoragePath ?? context.Version.FileName, cancellationToken);
            foreach (var (page, html) in tables)
            {
                context.Tables.Add(new SalesForgeOCR.Core.Entities.DocumentTable
                {
                    DocumentId = context.Document.Id,
                    VersionId = context.Version.Id,
                    PageNumber = page,
                    Html = html
                });
            }
            return ProcessingResult.Ok();
        }
    }
}
