using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class QueueView : UserControl, IView
    {
        private readonly System.Windows.Forms.ListView _lv;
        private readonly SalesForgeOCR.Application.Services.ProcessingJobService _jobSvc;
        public QueueView()
        {
            _jobSvc = (SalesForgeOCR.Application.Services.ProcessingJobService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Application.Services.ProcessingJobService));
            _lv = new ListView { Dock = DockStyle.Fill, View = View.Details };
            _lv.Columns.Add("Id", 150);
            _lv.Columns.Add("DocumentId", 150);
            _lv.Columns.Add("Status", 100);
            _lv.Columns.Add("Created", 150);
            _lv.Columns.Add("Started", 150);
            _lv.Columns.Add("Completed", 150);
            _lv.Columns.Add("Error", 200);
            Controls.Add(_lv);
            Load += async (s, e) => await RefreshAsync();
        }

        private async System.Threading.Tasks.Task RefreshAsync()
        {
            _lv.Items.Clear();
            if (_jobSvc == null) return;
            // No repository method for listing all jobs; this scans documents and retrieves per-document jobs as a pragmatic approach
            var docRepo = (SalesForgeOCR.Core.Interfaces.IDocumentRepository)Program.Host.Services.GetService(typeof(SalesForgeOCR.Core.Interfaces.IDocumentRepository));
            if (docRepo == null) return;
            var docs = await docRepo.SearchDocumentsAsync("", System.Threading.CancellationToken.None);
            foreach (var d in docs)
            {
                var jobs = await _jobSvc.GetByDocumentAsync(d.Id);
                foreach (var j in jobs)
                {
                    var li = new ListViewItem(new string[] { j.Id.ToString(), j.DocumentId.ToString(), j.Status.ToString(), j.CreatedAt.ToString("o"), j.StartedAt?.ToString("o") ?? "", j.CompletedAt?.ToString("o") ?? "", j.ErrorMessage ?? "" });
                    _lv.Items.Add(li);
                }
            }
        }

        public Control GetViewControl() => this;
    }
}
