using Microsoft.Extensions.DependencyInjection;
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
            _lstFiles.BringToFront();

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
            using var dlg = new OpenFileDialog { Multiselect = true, Filter = "OCR documents|*.pdf;*.png;*.jpg;*.jpeg;*.tif;*.tiff;*.bmp;*.txt;*.csv" };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                foreach (var f in dlg.FileNames) _lstFiles.Items.Add(f);
            }
        }

        private async void BtnQueue_Click(object? sender, EventArgs e)
        {
            var importer = Program.Host.Services.GetRequiredService<DocumentImportService>();
            _btnQueue.Enabled = false;
            var successful = new System.Collections.Generic.List<string>();
            foreach (var it in _lstFiles.Items.OfType<string>().ToArray())
            {
                try
                {
                    await importer.ImportAsync(it);
                    successful.Add(it);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to queue {it}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            foreach (var item in successful) _lstFiles.Items.Remove(item);
            _btnQueue.Enabled = true;
        }

        public Control GetViewControl() => this;
    }
}
