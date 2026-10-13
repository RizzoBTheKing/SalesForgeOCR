using SalesForgeOCR.Core.Interfaces;
using SalesForgeOCR.Core.Entities;
using SalesForgeOCR.UI.Services;
namespace SalesForgeOCR.UI.Forms;
public class SettingsView : UserControl, IView
{
    private readonly ISettingsService _settings;
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    public SettingsView(ISettingsService settings)
    {
        _settings = settings;
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Setting", ReadOnly = true });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Value" });
        Controls.Add(_grid);
        var save = new Button { Text = "Save settings", Dock = DockStyle.Bottom, Height = 36 };
        Controls.Add(save);_grid.BringToFront();
        Load += async (_, _) =>
        {
            try
            {
                var list = (await _settings.GetAllAsync()).Where(s => !s.Key.StartsWith("__")).ToList();
                if (!list.Any(s => s.Key == "Storage.MaxFileSizeBytes")) list.Add(new ApplicationSetting { Key = "Storage.MaxFileSizeBytes", Value = "104857600" });
                foreach (var item in list) _grid.Rows.Add(item.Key, item.Value);
            }
            catch (Exception error) { MessageBox.Show(error.Message, "Settings failed"); }
        };
        save.Click += async (_, _) =>
        {
            save.Enabled = false;
            try
            {
                _grid.EndEdit();
                foreach (DataGridViewRow row in _grid.Rows)
                {
                    var key = row.Cells[0].Value?.ToString() ?? "";
                    var value = row.Cells[1].Value?.ToString() ?? "";
                    if (key.StartsWith("__")) continue;
                    if (key == "Storage.MaxFileSizeBytes" && (!long.TryParse(value,out var size) || size <= 0)) throw new ArgumentException("File size limit must be a positive number of bytes.");
                    await _settings.SetAsync(new ApplicationSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
                }
                MessageBox.Show("Settings saved.", "Settings");
            }
            catch (Exception error) { MessageBox.Show(error.Message, "Save failed"); }
            finally { save.Enabled = true; }
        };
    }
    public Control GetViewControl() => this;
}
