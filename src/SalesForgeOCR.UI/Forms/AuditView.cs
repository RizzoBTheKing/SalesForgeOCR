using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class AuditView : UserControl, IView
    {
        private readonly ListView _lv;
        public AuditView()
        {
            _lv = new ListView { Dock = DockStyle.Fill, View = View.Details };
            _lv.Columns.Add("Timestamp", 150);
            _lv.Columns.Add("EventType", 150);
            _lv.Columns.Add("Actor", 100);
            _lv.Columns.Add("Details", 400);
            Controls.Add(_lv);
            Load += async (s, e) => await RefreshAsync();
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            _lv.Items.Clear();
            var connStr = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = "salesforgeocr.db" }.ToString();
            await using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
            await conn.OpenAsync();
            var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT Timestamp, EventType, Actor, Details FROM AuditLogs ORDER BY Timestamp DESC LIMIT 500";
            await using var r = await cmd.ExecuteReaderAsync();
            while (await r.ReadAsync())
            {
                _lv.Items.Add(new ListViewItem(new string[] { r.GetString(0), r.GetString(1), r.GetString(2), r.GetString(3) }));
            }
        }

        public Control GetViewControl() => this;
    }
}
