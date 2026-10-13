using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class MainForm : Form
    {
        private readonly IServiceProvider _sp;
        private readonly NavigationService _nav;
        private readonly Panel _contentPanel;
        private readonly ListBox _navList;

        public MainForm(IServiceProvider sp)
        {
            _sp = sp;
            Width = 1000;
            Height = 700;
            Text = "SalesForgeOCR - Admin";

            _navList = new ListBox { Dock = DockStyle.Left, Width = 200 };
            _navList.Items.AddRange(new object[] { "Dashboard", "Upload", "Explorer", "Search", "Queue", "Audit", "Settings" });
            _navList.SelectedIndexChanged += NavList_SelectedIndexChanged;
            Controls.Add(_navList);

            _contentPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(_contentPanel);
            _contentPanel.BringToFront();

            _nav = new NavigationService(_sp, _contentPanel);

            Shown += (s, e) => _nav.Navigate<DashboardView>();
        }

        private void NavList_SelectedIndexChanged(object? sender, EventArgs e)
        {
            switch (_navList.SelectedItem?.ToString())
            {
                case "Dashboard": _nav.Navigate<DashboardView>(); break;
                case "Upload": _nav.Navigate<UploadView>(); break;
                case "Explorer": _nav.Navigate<DocumentExplorerView>(); break;
                case "Search": _nav.Navigate<SearchView>(); break;
                case "Queue": _nav.Navigate<QueueView>(); break;
                case "Audit": _nav.Navigate<AuditView>(); break;
                case "Settings": _nav.Navigate<SettingsView>(); break;
            }
        }
    }
}
