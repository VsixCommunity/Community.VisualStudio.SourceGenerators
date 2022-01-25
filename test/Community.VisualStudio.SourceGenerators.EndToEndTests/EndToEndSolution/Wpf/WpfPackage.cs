using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Wpf
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSCommandTable.WpfPackage.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class WpfPackage : AsyncPackage
    {
        public override string ToString()
        {
            return $"{Vsix.Name}, MyCommand={VSCommandTable.WpfPackage.MyCommand}";
        }

        protected override WindowPane CreateToolWindow(Type toolWindowType, int id)
        {
            return new ToolWindowPane
            {
                Content = new WpfComponent()
            };
        }
    }
}
