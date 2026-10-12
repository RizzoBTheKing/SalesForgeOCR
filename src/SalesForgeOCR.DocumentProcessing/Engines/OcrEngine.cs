using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;
using Tesseract;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class OcrEngine : SalesForgeOCR.Core.Interfaces.IOcrEngine
    {
        private readonly string _tessDataPath;

        public OcrEngine(string? tessDataPath = null)
        {
            _tessDataPath = tessDataPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");
        }

        public async Task<string> ExtractTextAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            // If PDF, try PdfPig
            var ext = Path.GetExtension(imagePath)?.ToLowerInvariant();
            if (ext == ".pdf")
            {
                using var doc = PdfDocument.Open(imagePath);
                var writer = new System.Text.StringBuilder();
                foreach (var page in doc.GetPages())
                {
                    writer.AppendLine(page.Text);
                }
                return writer.ToString();
            }

            // For images, attempt Tesseract if available
            try
            {
                using var engine = new TesseractEngine(_tessDataPath, "eng", EngineMode.Default);
                using var img = Pix.LoadFromFile(imagePath);
                using var page = engine.Process(img);
                return page.GetText() ?? string.Empty;
            }
            catch
            {
                // If tesseract not available or fails, return empty string
                return string.Empty;
            }
        }

        public async Task<IEnumerable<string>> ExtractTextPagesAsync(string documentPath, CancellationToken cancellationToken = default)
        {
            var list = new List<string>();
            var ext = Path.GetExtension(documentPath)?.ToLowerInvariant();
            if (ext == ".pdf")
            {
                using var doc = PdfDocument.Open(documentPath);
                foreach (var page in doc.GetPages())
                {
                    list.Add(page.Text);
                }
            }
            else
            {
                var text = await ExtractTextAsync(documentPath, cancellationToken);
                list.Add(text);
            }
            return list;
        }
    }
}
