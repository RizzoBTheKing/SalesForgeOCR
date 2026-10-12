using System;
using System.Windows.Forms;
using SalesForgeOCR.UI.Services;
using SalesForgeOCR.Security;

namespace SalesForgeOCR.UI.Forms
{
    public class LoginForm : Form
    {
        private readonly TextBox _txtUser;
        private readonly TextBox _txtPass;
        private readonly Button _btnLogin;

        public LoginForm()
        {
            Width = 400;
            Height = 200;
            Text = "Login";
            _txtUser = new TextBox { PlaceholderText = "Username", Dock = DockStyle.Top };
            Controls.Add(_txtUser);
            _txtPass = new TextBox { PlaceholderText = "Password", Dock = DockStyle.Top, UseSystemPasswordChar = true };
            Controls.Add(_txtPass);
            _btnLogin = new Button { Text = "Login", Dock = DockStyle.Top };
            _btnLogin.Click += BtnLogin_Click;
            Controls.Add(_btnLogin);
        }

        private async void BtnLogin_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_txtPass.Text))
            {
                MessageBox.Show("Enter password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var auth = (SalesForgeOCR.Security.AuthenticationService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Security.AuthenticationService));
            var sessionMgr = (SalesForgeOCR.Security.SessionManager)Program.Host.Services.GetService(typeof(SalesForgeOCR.Security.SessionManager));
            var audit = (SalesForgeOCR.Core.Interfaces.IAuditService)Program.Host.Services.GetService(typeof(SalesForgeOCR.Core.Interfaces.IAuditService));

            try
            {
                var ok = await auth.VerifyMasterPasswordAsync(_txtPass.Text);
                if (!ok)
                {
                    if (audit != null) await audit.LogAsync(new SalesForgeOCR.Core.Entities.AuditLog { EventType = "Auth.Failure", Actor = "user", Details = "Failed login attempt" });
                    MessageBox.Show("Invalid password.", "Login", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // create session
                var session = await auth.CreateSessionAsync("admin");
                if (sessionMgr != null) await sessionMgr.CreateAsync(session);
                if (audit != null) await audit.LogAsync(new SalesForgeOCR.Core.Entities.AuditLog { EventType = "Auth.Success", Actor = "admin", Details = "User logged in" });

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Login error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
