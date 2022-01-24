using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace Legacy
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(Generated.PackageGuids.LegacyPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class LegacyPackage : AsyncPackage
    {
        public override string ToString()
        {
            return $"{Generated.Vsix.Name}, MyCommand={Generated.PackageIds.MyCommand}";
        }
    }
}
