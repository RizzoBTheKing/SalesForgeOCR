using System;
using System.Linq;
using System.Windows.Forms;
using SalesForgeOCR.Core.Search;
using SalesForgeOCR.Search;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class SearchView : UserControl, IView
    {
        private readonly TextBox _txtQuery;
        private readonly Button _btnSearch;
        private readonly ListBox _lstResults;
        private readonly SearchService _search;

        public SearchView(SearchService search)
        {
            _search = search;
            _txtQuery = new TextBox { Dock = DockStyle.Top };
            Controls.Add(_txtQuery);
            _btnSearch = new Button { Text = "Search", Dock = DockStyle.Top };
            _btnSearch.Click += BtnSearch_Click;
            Controls.Add(_btnSearch);
            _lstResults = new ListBox { Dock = DockStyle.Fill };
            _lstResults.DoubleClick += LstResults_DoubleClick;
            Controls.Add(_lstResults);
        }

        private async void BtnSearch_Click(object? sender, EventArgs e)
        {
            var req = new SearchRequest { Query = _txtQuery.Text, MatchType = SearchMatchType.Prefix };
            var res = await _search.SearchAsync(req);
            _lstResults.Items.Clear();
            foreach (var r in res.Results.SelectMany(x => x.Hits.Select(h => (new { Display = $"{x.DocumentName} [{h.SourceType}] {h.MatchValue}", DocumentId = x.DocumentId, Hit = h }))))
            {
                _lstResults.Items.Add(r);
            }
        }

        private void LstResults_DoubleClick(object? sender, EventArgs e)
        {
            var sel = _lstResults.SelectedItem;
            if (sel == null) return;
            var docIdProp = sel.GetType().GetProperty("DocumentId");
            if (docIdProp == null) return;
            var docId = (Guid)docIdProp.GetValue(sel)!;
            var viewer = (SalesForgeOCR.UI.Forms.DocumentViewerForm)Program.Host.Services.GetService(typeof(SalesForgeOCR.UI.Forms.DocumentViewerForm));
            if (viewer != null)
            {
                viewer.LoadDocument(docId);
                var form = new Form { Text = "Document Viewer", Width = 900, Height = 700 };
                form.Controls.Add(viewer.GetViewControl());
                form.ShowDialog();
            }
        }

        public Control GetViewControl() => this;
    }
}
