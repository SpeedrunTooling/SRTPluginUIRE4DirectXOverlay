using SRTPluginBase;
using System;

namespace SRTPluginUIRE4DirectXOverlay
{
    internal class PluginInfo : PluginInfoBase, IPluginInfo
    {
        public override string Name => "DirectX Overlay UI (Resident Evil 4 Remake (2023))";

        public override string Description => "A DirectX-based Overlay User Interface for displaying Resident Evil 4 Remake (2023) UI. (SRT Host 4.0)";

        public override string Author => "VideoGameRoulette, Squirrelies";

        public override Uri MoreInfoURL => new Uri("https://github.com/SpeedrunTooling/SRTPluginUIRE4DirectXOverlay");

        public override int VersionMajor => Version.Major;

        public override int VersionMinor => Version.Minor;

        public override int VersionBuild => Version.Build;

        public override int VersionRevision => Version.Revision;

#pragma warning disable IDE1006 // Naming Styles
		internal static PluginInfo Default = new PluginInfo();
#pragma warning restore IDE1006 // Naming Styles
	}
}
