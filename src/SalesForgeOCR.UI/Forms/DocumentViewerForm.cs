using System;
using System.Linq;
using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class DocumentViewerForm : UserControl, IView
    {
        private Guid _documentId;
        private readonly TabControl _tabs;
        private readonly ListBox _lstVersions;
        private readonly ListBox _lstTexts;
        private readonly ListBox _lstEntities;
        private readonly ListBox _lstMetadata;
        private readonly ListBox _lstImages;
        private readonly ListBox _lstJobs;
        private readonly ListBox _lstAudit;

        public DocumentViewerForm()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            _lstVersions = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Versions") { Controls = { _lstVersions } });
            _lstTexts = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Extracted Text") { Controls = { _lstTexts } });
            _lstEntities = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Entities") { Controls = { _lstEntities } });
            _lstMetadata = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Metadata") { Controls = { _lstMetadata } });
            _lstImages = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Images") { Controls = { _lstImages } });
            _lstJobs = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Processing History") { Controls = { _lstJobs } });
            _lstAudit = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Audit") { Controls = { _lstAudit } });

            Controls.Add(_tabs);
        }

        public void LoadDocument(Guid documentId)
        {
            _documentId = documentId;
            _ = LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var svc = (SalesForgeOCR.Application.Services.DocumentRetrievalService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Application.Services.DocumentRetrievalService));
                var auditRepo = (SalesForgeOCR.Core.Interfaces.IAuditService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Core.Interfaces.IAuditService));
                if (svc == null) return;
                var data = await svc.GetDocumentViewAsync(_documentId);

                _lstVersions.Items.Clear();
                foreach (var v in data.versions) _lstVersions.Items.Add($"{v.VersionNumber} - {v.FileName} - {v.CreatedAt:o}");

                _lstTexts.Items.Clear();
                foreach (var t in data.texts) _lstTexts.Items.Add($"Page {t.PageNumber}: {t.Content}");

                _lstEntities.Items.Clear();
                foreach (var e in data.entities) _lstEntities.Items.Add($"{e.Type}: {e.Value} (Page {e.PageNumber})");

                _lstMetadata.Items.Clear();
                foreach (var m in data.metadata) _lstMetadata.Items.Add($"{m.Key}: {m.Value}");

                _lstImages.Items.Clear();
                foreach (var im in data.images) _lstImages.Items.Add($"Page {im.PageNumber}: {im.FilePath}");

                _lstJobs.Items.Clear();
                foreach (var j in data.jobs) _lstJobs.Items.Add($"{j.Id} - {j.Status} - Created: {j.CreatedAt:o} - Error: {j.ErrorMessage}");

                // load audit logs directly from repository if available
                var repo = Program.Host.Services.GetService(typeof(SalesForgeOCR.Data.Repositories.AuditRepository)) as SalesForgeOCR.Data.Repositories.AuditRepository;
                if (repo != null)
                {
                    // simple query: get all audit logs and filter by document id in details
                    var connStr = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder { DataSource = "salesforgeocr.db" }.ToString();
                    await using var conn = new Microsoft.Data.Sqlite.SqliteConnection(connStr);
                    await conn.OpenAsync();
                    var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT Timestamp, EventType, Actor, Details FROM AuditLogs WHERE Details LIKE $like ORDER BY Timestamp DESC LIMIT 100";
                    cmd.Parameters.AddWithValue("$like", "%" + _documentId.ToString() + "%");
                    await using var r = await cmd.ExecuteReaderAsync();
                    while (await r.ReadAsync())
                    {
                        _lstAudit.Items.Add($"{r.GetString(0)} - {r.GetString(1)} - {r.GetString(2)} - {r.GetString(3)}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Viewer Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Control GetViewControl() => this;
    }
}
