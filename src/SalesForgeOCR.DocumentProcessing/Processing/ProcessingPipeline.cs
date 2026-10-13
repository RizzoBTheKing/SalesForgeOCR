using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public class ProcessingPipeline
    {
        private readonly IList<ProcessingStage> _stages = new List<ProcessingStage>();

        public ProcessingPipeline AddStage(ProcessingStage stage)
        {
            _stages.Add(stage);
            return this;
        }

        public async Task<ProcessingResult> ExecuteAsync(ProcessingContext context, CancellationToken cancellationToken = default)
        {
            foreach (var stage in _stages)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var res = await stage.ExecuteAsync(context, cancellationToken);
                if (!res.Success)
                {
                    return res;
                }
            }
            return ProcessingResult.Ok();
        }
    }
}
