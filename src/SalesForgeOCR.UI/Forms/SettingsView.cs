using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class SettingsView : UserControl, IView
    {
        private readonly SalesForgeOCR.Infrastructure.Configuration.SettingsService _settings;
        private readonly ListView _lv;
        private readonly Button _btnSave;
        public SettingsView()
        {
            _settings = (SalesForgeOCR.Infrastructure.Configuration.SettingsService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Infrastructure.Configuration.SettingsService));
            _lv = new ListView { Dock = DockStyle.Fill, View = View.Details };
            _lv.Columns.Add("Key",200);
            _lv.Columns.Add("Value",400);
            Controls.Add(_lv);

            _btnSave = new Button { Text = "Save", Dock = DockStyle.Bottom };
            _btnSave.Click += async (s,e) => await SaveAsync();
            Controls.Add(_btnSave);

            Load += async (s,e)=> await LoadAsync();
        }

        private async System.Threading.Tasks.Task LoadAsync()
        {
            _lv.Items.Clear();
            if (_settings == null) return;
            var list = await _settings.GetAllAsync();
            foreach(var setting in list)
            {
                _lv.Items.Add(new ListViewItem(new string[]{setting.Key, setting.Value}));
            }
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (_settings == null) return;
            foreach(ListViewItem item in _lv.Items)
            {
                var key = item.SubItems[0].Text;
                var value = item.SubItems[1].Text;
                var s = new SalesForgeOCR.Core.Entities.ApplicationSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow };
                await _settings.SetAsync(s);
            }
            MessageBox.Show("Settings saved.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public Control GetViewControl() => this;
    }
}
