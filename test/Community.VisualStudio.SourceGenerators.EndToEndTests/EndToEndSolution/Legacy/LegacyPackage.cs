using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Legacy
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.LegacyPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class LegacyPackage : AsyncPackage
    {
        public override string ToString()
        {
            return $"{Vsix.Name}, MyCommand={PackageIds.MyCommand}";
        }
    }
}
