using System;
using System.Collections.Generic;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public class ProcessorFactory
    {
        private readonly IList<ProcessingStage> _registered = new List<ProcessingStage>();

        public void Register(ProcessingStage stage)
        {
            if (stage == null) throw new ArgumentNullException(nameof(stage));
            _registered.Add(stage);
        }

        public IEnumerable<ProcessingStage> CreateDefaultPipeline()
        {
            // Consumers should register their own implementations. Return registered stages in order.
            return _registered;
        }
    }
}
