using System;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;

namespace SalesForgeOCR.UI.Services
{
    public class NavigationService
    {
        private readonly IServiceProvider _sp;
        private readonly Panel _hostPanel;

        public NavigationService(IServiceProvider sp, Panel hostPanel)
        {
            _sp = sp;
            _hostPanel = hostPanel;
        }

        public void Navigate<T>() where T : IView
        {
            var view = (IView)_sp.GetService(typeof(T))!;
            ShowView(view);
        }

        public void ShowView(IView view)
        {
            var ctrl = view.GetViewControl();
            _hostPanel.Controls.Clear();
            ctrl.Dock = DockStyle.Fill;
            _hostPanel.Controls.Add(ctrl);
            ctrl.Focus();
        }
    }
}
