using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UglyToad.PdfPig;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class MetadataExtractor : SalesForgeOCR.Core.Interfaces.IMetadataExtractor
    {
        public async Task<IEnumerable<(string key, string value)>> ExtractMetadataAsync(string documentPath, CancellationToken cancellationToken = default)
        {
            var list = new List<(string, string)>();
            try
            {
                var fi = new FileInfo(documentPath);
                list.Add(("FileName", fi.Name));
                list.Add(("Size", fi.Length.ToString()));
                list.Add(("Created", fi.CreationTimeUtc.ToString("o")));

                var ext = Path.GetExtension(documentPath)?.ToLowerInvariant();
                if (ext == ".pdf")
                {
                    using var doc = PdfDocument.Open(documentPath);
                    var info = doc.Information;
                    if (!string.IsNullOrWhiteSpace(info.Title)) list.Add(("Title", info.Title));
                    if (!string.IsNullOrWhiteSpace(info.Author)) list.Add(("Author", info.Author));
                }
            }
            catch
            {
                // ignore
            }
            return list;
        }
    }
}
