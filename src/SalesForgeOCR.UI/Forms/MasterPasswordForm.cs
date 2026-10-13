using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Forms;

namespace SalesForgeOCR.UI.Forms
{
    public class MasterPasswordForm : Form
    {
        private readonly TextBox _txtPass;
        private readonly TextBox _txtPassConfirm;
        private readonly Button _btnCreate;

        public MasterPasswordForm()
        {
            Width = 400;
            Height = 220;
            Text = "Create Master Password";
            _txtPass = new TextBox { PlaceholderText = "Password", Dock = DockStyle.Top, UseSystemPasswordChar = true };
            Controls.Add(_txtPass);
            _txtPassConfirm = new TextBox { PlaceholderText = "Confirm", Dock = DockStyle.Top, UseSystemPasswordChar = true };
            Controls.Add(_txtPassConfirm);
            _btnCreate = new Button { Text = "Create", Dock = DockStyle.Top };
            _btnCreate.Click += BtnCreate_Click;
            Controls.Add(_btnCreate);
        }

        private async void BtnCreate_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtPass.Text) || _txtPass.Text != _txtPassConfirm.Text)
            {
                MessageBox.Show("Passwords must match and not be empty.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Use MasterPasswordSetupService to persist master password
            var svc = Program.Host.Services.GetRequiredService<SalesForgeOCR.Security.MasterPasswordSetupService>();
            if (svc == null)
            {
                MessageBox.Show("Setup service not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                await svc.SetupAsync(_txtPass.Text);
                MessageBox.Show("Master password created.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
