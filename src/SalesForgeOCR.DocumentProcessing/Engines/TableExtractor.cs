using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ClosedXML.Excel;

namespace SalesForgeOCR.DocumentProcessing.Engines
{
    public class TableExtractor : SalesForgeOCR.Core.Interfaces.ITableExtractor
    {
        public async Task<IEnumerable<(int page, string html)>> ExtractTablesAsync(string documentPath, CancellationToken cancellationToken = default)
        {
            var ext = Path.GetExtension(documentPath)?.ToLowerInvariant();
            var result = new List<(int, string)>();
            if (ext == ".xlsx" || ext == ".xls" || ext == ".xlsm")
            {
                using var wb = new XLWorkbook(documentPath);
                var page = 1;
                foreach (var ws in wb.Worksheets)
                {

                    // Simple HTML conversion
                    var html = new System.Text.StringBuilder();
                    html.AppendLine("<table>");
                    var rows = ws.RangeUsed()?.RowsUsed();
                    if (rows != null)
                    {
                        foreach (var r in rows)
                        {
                            html.AppendLine("<tr>");
                            foreach (var c in r.Cells())
                            {
                                html.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(c.GetValue<string>())}</td>");
                            }
                            html.AppendLine("</tr>");
                        }
                    }
                    html.AppendLine("</table>");
                    result.Add((page++, html.ToString()));
                }
            }
            else if (ext == ".csv" || ext == ".txt")
            {
                var lines = await File.ReadAllLinesAsync(documentPath, cancellationToken);
                var html = new System.Text.StringBuilder();
                html.AppendLine("<table>");
                foreach (var line in lines)
                {
                    html.AppendLine("<tr>");
                    var cols = line.Split(',');
                    foreach (var col in cols)
                    {
                        html.AppendLine($"<td>{System.Net.WebUtility.HtmlEncode(col)}</td>");
                    }
                    html.AppendLine("</tr>");
                }
                html.AppendLine("</table>");
                result.Add((1, html.ToString()));
            }

            return result;
        }
    }
}
