using System;
using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class FirstRunWizardForm : Form
    {
        private readonly Button _btnStart;
        public FirstRunWizardForm()
        {
            Width = 500;
            Height = 300;
            Text = "First Run Wizard";
            _btnStart = new Button { Text = "Create Master Password", Dock = DockStyle.Top };
            _btnStart.Click += (s, e) => {
                var mp = (SalesForgeOCR.UI.Forms.MasterPasswordForm)Program.Host.Services.GetService(typeof(SalesForgeOCR.UI.Forms.MasterPasswordForm));
                if (mp.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            };
            Controls.Add(_btnStart);
        }
    }
}
