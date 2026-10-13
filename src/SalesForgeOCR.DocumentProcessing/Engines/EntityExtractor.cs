using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class RegexEntityExtractor : SalesForgeOCR.Core.Interfaces.IEntityExtractor
    {
        private static readonly Regex Email = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        private static readonly Regex Phone = new Regex(@"\+?[0-9][0-9\- ()]{7,}[0-9]", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        private static readonly Regex Date = new Regex(@"\b\d{1,2}[\-/]\d{1,2}[\-/]\d{2,4}\b", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        private static readonly Regex Amount = new Regex(@"\b\$?\d{1,3}(,\d{3})*(\.\d{2})?\b", RegexOptions.Compiled, TimeSpan.FromSeconds(2));
        private static readonly Regex Vin = new Regex(@"\b([A-HJ-NPR-Z0-9]{17})\b", RegexOptions.Compiled, TimeSpan.FromSeconds(2));

        public Task<IEnumerable<(string type, string value, double confidence, int page)>> ExtractEntitiesAsync(string text, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var list = new List<(string, string, double, int)>();
            if (string.IsNullOrWhiteSpace(text)) return Task.FromResult((IEnumerable<(string, string, double, int)>)list);

            foreach (Match m in Email.Matches(text)) list.Add(("Email", m.Value, 0.99, 1));
            foreach (Match m in Phone.Matches(text)) list.Add(("Phone", m.Value, 0.9, 1));
            foreach (Match m in Date.Matches(text)) list.Add(("Date", m.Value, 0.8, 1));
            foreach (Match m in Amount.Matches(text)) list.Add(("Amount", m.Value, 0.8, 1));
            foreach (Match m in Vin.Matches(text)) list.Add(("VIN", m.Value, 0.95, 1));

            return Task.FromResult((IEnumerable<(string, string, double, int)>)list);
        }
    }
}
