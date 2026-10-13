using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class DocumentViewerForm : UserControl, IView
    {
        private Guid _documentId;
        private string? _sourcePath;
        private readonly RichTextBox _code = new() { Dock=DockStyle.Fill,ReadOnly=true,WordWrap=false,Font=new System.Drawing.Font("Consolas",10) };
        private readonly TextBox _methodName=new() { Text="GenerateImportedDocument",Width=250 };
        private readonly PictureBox _imagePreview=new() { Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom };

        private readonly TabControl _tabs;
        private readonly ListBox _lstVersions;
        private readonly RichTextBox _textContent;
        private readonly RichTextBox _tableContent;
        private readonly ListBox _lstEntities;
        private readonly ListBox _lstMetadata;
        private readonly ListBox _lstImages;
        private readonly ListBox _lstJobs;
        private readonly ListBox _lstAudit;

        public DocumentViewerForm()
        {
            _tabs = new TabControl { Dock = DockStyle.Fill };

            _lstVersions = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Versions") { Controls = { _lstVersions } });
            _textContent = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = new System.Drawing.Font("Segoe UI", 11) };
            _tabs.TabPages.Add(new TabPage("Extracted Text") { Controls = { _textContent } });
            _tableContent = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true };
            _tabs.TabPages.Add(new TabPage("Tables") { Controls = { _tableContent } });
            _lstEntities = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Entities") { Controls = { _lstEntities } });
            _lstMetadata = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Metadata") { Controls = { _lstMetadata } });
            _lstImages = new ListBox { Dock = DockStyle.Left,Width=260 };
            var imageTab=new TabPage("Images");imageTab.Controls.Add(_imagePreview);imageTab.Controls.Add(_lstImages);_imagePreview.BringToFront();_tabs.TabPages.Add(imageTab);
            _lstImages.SelectedIndexChanged += (_, _) =>
            {
                _imagePreview.Image?.Dispose();_imagePreview.Image=null;
                if(_lstImages.SelectedItem is SalesForgeOCR.Core.Entities.DocumentImage image)
                {
                    try { using var source=System.Drawing.Image.FromFile(image.FilePath);_imagePreview.Image=new System.Drawing.Bitmap(source); }
                    catch(Exception error) { MessageBox.Show(error.Message,"Image preview failed"); }
                }
            };
            _lstImages.DisplayMember="DisplayLabel";
            var codeTab=new TabPage("SalesForge code");
            var tools=new FlowLayoutPanel { Dock=DockStyle.Top,Height=42,WrapContents=false };
            var generate=new Button {Text="Generate code",AutoSize=true};
            var copy=new Button {Text="Copy code",AutoSize=true};
            var saveCode=new Button {Text="Export code + helper",AutoSize=true};
            tools.Controls.AddRange(new Control[]{new Label {Text="Method name",AutoSize=true,Padding=new Padding(0,8,0,0)},_methodName,generate,copy,saveCode});
            var guidance=new Label { Dock=DockStyle.Bottom,Height=44,Text="Word/Excel use installed Office once to preserve page layout. Exported code uses PDFsharp only. Use {{CustomerName}}, {{Branch}}, {{SalesExecutive}}, {{ChassisNumber}}, {{Make}}, {{Model}}, {{DeliveryDate}} or {{SellingPrice}} for live values; completed documents retain their static text." };
            codeTab.Controls.Add(guidance);codeTab.Controls.Add(_code);codeTab.Controls.Add(tools);_code.BringToFront();_tabs.TabPages.Add(codeTab);
            generate.Click += async (_, _) =>
            {
                if(_sourcePath==null) {MessageBox.Show("This document has no stored source file.");return;}
                generate.Enabled=false;
                try { _code.Text=await Program.Host.Services.GetRequiredService<SalesForgeOCR.DocumentProcessing.Engines.DocumentCodeGenerator>().GenerateAsync(_sourcePath,_methodName.Text.Trim()); }
                catch(Exception error) {MessageBox.Show(error.Message,"Code generation failed");}
                finally {generate.Enabled=true;}
            };
            copy.Click += (_, _) => { if(_code.TextLength>0) {try {Clipboard.SetText(_code.Text);}catch(Exception error){MessageBox.Show(error.Message,"Copy failed");}} };
            saveCode.Click += (_, _) =>
            {
                if(_code.TextLength==0){MessageBox.Show("Generate the code first.");return;}
                using var dialog=new SaveFileDialog {Filter="C# source|*.cs",FileName=_methodName.Text.Trim()+".cs"};
                if(dialog.ShowDialog()!=DialogResult.OK)return;
                try
                {
                    var helpers=new[]{"PdfFormDocument.cs","PdfLayoutProfile.cs"};
                    if(helpers.Contains(System.IO.Path.GetFileName(dialog.FileName),StringComparer.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Choose a method filename other than a renderer helper name.");
                    System.IO.File.WriteAllText(dialog.FileName,"namespace SalesForge;\npublic partial class SalesForge\n{\n"+_code.Text+"\n}\n",System.Text.Encoding.UTF8);
                    foreach(var name in helpers)
                    {
                        var helperTarget=System.IO.Path.Combine(System.IO.Path.GetDirectoryName(dialog.FileName)!,name);
                        if(!System.IO.File.Exists(helperTarget))System.IO.File.Copy(System.IO.Path.Combine(AppContext.BaseDirectory,"CodeTemplates",name+".txt"),helperTarget);
                    }
                    MessageBox.Show("Code exported. Add the generated partial class to SalesForge. The renderer helper is included for projects that do not have it yet. Review all static text and field mappings.","Code exported");
                }
                catch(Exception error){MessageBox.Show(error.Message,"Code export failed");}
            };
            _lstJobs = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Processing History") { Controls = { _lstJobs } });
            _lstAudit = new ListBox { Dock = DockStyle.Fill }; _tabs.TabPages.Add(new TabPage("Audit") { Controls = { _lstAudit } });

            Controls.Add(_tabs);
            var export = new Button { Text = "Export extracted text", Dock = DockStyle.Bottom, Height = 36 };
            export.Click += (sender, args) =>
            {
                using var dialog = new SaveFileDialog { Filter = "Text file|*.txt", FileName = "extracted-text.txt" };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try { System.IO.File.WriteAllText(dialog.FileName, _textContent.Text, System.Text.Encoding.UTF8); }
                    catch (Exception error) { MessageBox.Show(error.Message, "Export failed"); }
                }
            };
            Controls.Add(export);
            _tabs.BringToFront();
        }

        public void LoadDocument(Guid documentId)
        {
            _documentId = documentId;
            Dock = DockStyle.Fill;
            _ = LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            try
            {
                var svc = Program.Host.Services.GetRequiredService<SalesForgeOCR.Application.Services.DocumentRetrievalService>();
                var auditRepo = Program.Host.Services.GetRequiredService<SalesForgeOCR.Core.Interfaces.IAuditService>();
                if (svc == null) return;
                var data = await svc.GetDocumentViewAsync(_documentId);
                _sourcePath=data.versions.FirstOrDefault(v=>v.Id==data.doc.CurrentVersionId)?.StoragePath ?? data.versions.FirstOrDefault()?.StoragePath;
                _code.Clear();

                _lstVersions.Items.Clear();
                foreach (var v in data.versions) _lstVersions.Items.Add($"{v.VersionNumber} - {v.FileName} - {v.CreatedAt:o}");

                _textContent.Text = string.Join(Environment.NewLine + Environment.NewLine, data.texts.OrderBy(t => t.PageNumber).Select(t => $"Page {t.PageNumber}{Environment.NewLine}{t.Content}"));
                _tableContent.Text = string.Join(Environment.NewLine, data.tables.Select(t => $"Page {t.PageNumber}{Environment.NewLine}{t.Html}"));
                _lstAudit.Items.Clear();

                _lstEntities.Items.Clear();
                foreach (var e in data.entities) _lstEntities.Items.Add($"{e.Type}: {e.Value} (Page {e.PageNumber})");

                _lstMetadata.Items.Clear();
                foreach (var m in data.metadata) _lstMetadata.Items.Add($"{m.Key}: {m.Value}");

                _lstImages.Items.Clear();
                foreach (var im in data.images) _lstImages.Items.Add(im);

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

        protected override void Dispose(bool disposing)
        {
            if(disposing)_imagePreview.Image?.Dispose();
            base.Dispose(disposing);
        }
        public Control GetViewControl() => this;
    }
}
