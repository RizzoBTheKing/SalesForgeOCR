using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SalesForgeOCR.UI.Services;
using SalesForgeOCR.Application.Services;
using SalesForgeOCR.Core.Entities;
using System.Threading.Tasks;

namespace SalesForgeOCR.UI.Forms
{
    public class UploadView : UserControl, IView
    {
        private readonly Button _btnSelect;
        private readonly ListBox _lstFiles;
        private readonly Button _btnQueue;

        public UploadView()
        {
            _btnSelect = new Button { Text = "Select Files", Dock = DockStyle.Top };
            _btnSelect.Click += BtnSelect_Click;
            Controls.Add(_btnSelect);

            _lstFiles = new ListBox { Dock = DockStyle.Fill };
            Controls.Add(_lstFiles);

            _btnQueue = new Button { Text = "Queue for Processing", Dock = DockStyle.Bottom };
            _btnQueue.Click += BtnQueue_Click;
            Controls.Add(_btnQueue);

            AllowDrop = true;
            DragEnter += UploadView_DragEnter;
            DragDrop += UploadView_DragDrop;
        }

        private void UploadView_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
                foreach (var f in files) _lstFiles.Items.Add(f);
            }
        }

        private void UploadView_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true) e.Effect = DragDropEffects.Copy;
        }

        private void BtnSelect_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog { Multiselect = true };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (var f in dlg.FileNames) _lstFiles.Items.Add(f);
            }
        }

        private async void BtnQueue_Click(object? sender, EventArgs e)
        {
            var docSvc = (SalesForgeOCR.Application.Services.DocumentService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Application.Services.DocumentService));
            var jobSvc = (SalesForgeOCR.Application.Services.ProcessingJobService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Application.Services.ProcessingJobService));
            if (docSvc == null || jobSvc == null)
            {
                MessageBox.Show("Processing services unavailable.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            foreach (var it in _lstFiles.Items.OfType<string>())
            {
                try
                {
                    var fi = new System.IO.FileInfo(it);
                    var doc = await docSvc.CreateDocumentAsync(fi.Name, fi.Length, "admin");
                    var ver = new SalesForgeOCR.Core.Entities.DocumentVersion { Id = Guid.NewGuid(), DocumentId = doc.Id, VersionNumber = 1, FileName = fi.Name, Size = fi.Length, CreatedAt = DateTime.UtcNow, CreatedBy = "admin" };
                    await docSvc.AddVersionAsync(doc.Id, ver);

                    var job = new SalesForgeOCR.Core.Entities.ProcessingJob { DocumentId = doc.Id, VersionId = ver.Id, CreatedBy = "admin" };
                    await jobSvc.CreateAsync(job);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to queue {it}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            _lstFiles.Items.Clear();
        }

        public Control GetViewControl() => this;
    }
}
