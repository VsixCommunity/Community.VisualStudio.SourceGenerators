using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace VsixSynchronizerFormat
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuids.VsixSynchronizerFormatPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class VsixSynchronizerFormatPackage : AsyncPackage
    {
        public override string ToString()
        {
            return $"{Vsix.Name}, MyCommand={PackageIds.MyCommand}";
        }
    }
}
