using Microsoft.Data.Sqlite;
using SalesForgeOCR.UI.Services;
namespace SalesForgeOCR.UI.Forms;
public class DashboardView : UserControl, IView
{
    private readonly Label _summary = new() { Dock = DockStyle.Fill, Font = new System.Drawing.Font("Segoe UI", 14), Padding = new Padding(24) };
    public DashboardView()
    {
        Controls.Add(_summary);
        VisibleChanged += async (_, _) => { if (Visible) await RefreshAsync(); };
        Load += async (_, _) => await RefreshAsync();
    }
    private async Task RefreshAsync()
    {
        try
        {
            await using var connection = new SqliteConnection("Data Source=salesforgeocr.db");
            await connection.OpenAsync();
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT (SELECT COUNT(*) FROM Documents), (SELECT COUNT(*) FROM ProcessingJobs WHERE Status=2), (SELECT COUNT(*) FROM ProcessingJobs WHERE Status IN (0,1)), (SELECT COUNT(*) FROM ProcessingJobs WHERE Status=3)";
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            _summary.Text = $"SalesForgeOCR\n\nDocuments: {reader.GetInt64(0)}\nCompleted jobs: {reader.GetInt64(1)}\nQueued or processing: {reader.GetInt64(2)}\nFailed jobs: {reader.GetInt64(3)}\n\nUpload documents, check their processing status, then open Explorer to read or export the text.";
        }
        catch (Exception error) { _summary.Text = "Could not load dashboard: " + error.Message; }
    }
    public Control GetViewControl() => this;
}
