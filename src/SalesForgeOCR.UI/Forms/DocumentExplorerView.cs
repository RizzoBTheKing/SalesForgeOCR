using Microsoft.Extensions.DependencyInjection;
using SalesForgeOCR.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.Application.Services;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class DocumentExplorerView : UserControl, IView
    {
        private readonly ListBox _lstDocs;
        private readonly Button _btnRefresh;

        public DocumentExplorerView()
        {
            _btnRefresh = new Button { Text = "Refresh", Dock = DockStyle.Top };
            _btnRefresh.Click += async (s, e) => await LoadDocsAsync();
            Controls.Add(_btnRefresh);

            _lstDocs = new ListBox { Dock = DockStyle.Fill, DisplayMember = "FileName" };
            VisibleChanged += async (s, e) => { if (Visible) await LoadDocsAsync(); };
            _lstDocs.DoubleClick += LstDocs_DoubleClick;
            Controls.Add(_lstDocs);
            _lstDocs.BringToFront();
        }

        private async void LstDocs_DoubleClick(object? sender, EventArgs e)
        {
            if (_lstDocs.SelectedItem is SalesForgeOCR.Core.Entities.Document doc)
            {
                var viewer = Program.Host.Services.GetRequiredService<SalesForgeOCR.UI.Forms.DocumentViewerForm>();
                if (viewer != null)
                {
                    // show viewer dialog with document id
                    viewer.LoadDocument(doc.Id);
                    var form = new Form { Text = "Document Viewer", Width = 900, Height = 700 };
                    form.Controls.Add(viewer.GetViewControl());
                    form.ShowDialog();
                }
            }
        }

        private async System.Threading.Tasks.Task LoadDocsAsync()
        {
            try
            {
                var repo = Program.Host.Services.GetService(typeof(IDocumentRepository)) as IDocumentRepository;
                if (repo == null) return;
                var docs = await repo.SearchDocumentsAsync("", System.Threading.CancellationToken.None);
                _lstDocs.Items.Clear();
                foreach (var d in docs) _lstDocs.Items.Add(d);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public Control GetViewControl() => this;
    }
}
