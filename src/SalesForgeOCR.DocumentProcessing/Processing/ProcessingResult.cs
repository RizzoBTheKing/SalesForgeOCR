using System;

namespace SalesForgeOCR.DocumentProcessing.Processing
{
    public class ProcessingResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Exception? Exception { get; set; }

        public static ProcessingResult Ok() => new ProcessingResult { Success = true };
        public static ProcessingResult Fail(string message, Exception? ex = null) => new ProcessingResult { Success = false, Message = message, Exception = ex };
    }
}
