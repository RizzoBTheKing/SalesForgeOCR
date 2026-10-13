using SalesForgeOCR.Core.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing.Stages
{
    public class ValidationStage : ProcessingStage
    {
        private readonly ISettingsService _settings;
        public ValidationStage(ISettingsService settings)
        {
            _settings = settings;
        }

        public override string Name => "Validation";

        public override async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = context.Version.StoragePath ?? context.Version.FileName;
            if (!System.IO.File.Exists(path)) return ProcessingResult.Fail("Stored document is missing.");
            if (new System.IO.FileInfo(path).Length == 0) return ProcessingResult.Fail("Document is empty.");
            // Basic validation: file size limits etc. Could read settings for limits
            var maxSizeSetting = await _settings.GetAsync("Storage.MaxFileSizeBytes", cancellationToken);
            if (maxSizeSetting != null && long.TryParse(maxSizeSetting.Value, out var maxBytes))
            {
                if (context.Version.Size > maxBytes)
                {
                    return ProcessingResult.Fail($"File exceeds configured max size ({maxBytes} bytes)");
                }
            }

            return ProcessingResult.Ok();
        }
    }
}
