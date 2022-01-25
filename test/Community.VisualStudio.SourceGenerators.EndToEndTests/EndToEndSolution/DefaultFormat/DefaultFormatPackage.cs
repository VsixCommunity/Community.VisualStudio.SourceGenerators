using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace DefaultFormat
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(VSCommandTable.DefaultFormatPackage.GuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class DefaultFormatPackage : AsyncPackage
    {
        public override string ToString()
        {
            return $"{Vsix.Name}, MyCommand={VSCommandTable.DefaultFormatPackage.MyCommand}";
        }
    }
}
