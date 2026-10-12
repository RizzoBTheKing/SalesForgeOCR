using System.Windows.Forms;
using SalesForgeOCR.UI.Services;

namespace SalesForgeOCR.UI.Forms
{
    public class DashboardView : UserControl, IView
    {
        private readonly Label _lbl;
        public DashboardView()
        {
            _lbl = new Label { Text = "Dashboard", Dock = DockStyle.Top, Height = 40 };
            Controls.Add(_lbl);
        }

        public Control GetViewControl() => this;
    }
}
