using SRTPluginBase;
using System;

namespace SRTPluginUIRE4DirectXOverlay
{
    internal class PluginInfo : IPluginInfo
    {
        public string Name => "DirectX Overlay UI (Resident Evil 4 Remake (2023))";

        public string Description => "A DirectX-based Overlay User Interface for displaying Resident Evil 4 Remake (2023) UI. (SRT Host 4.0)";

        public string Author => "VideoGameRoulette & Squirrelies";

        public Uri MoreInfoURL => new Uri("https://github.com/SpeedrunTooling/SRTPluginUIRE4DirectXOverlay");

		public int VersionMajor => assemblyVersion?.Major ?? 0;

		public int VersionMinor => assemblyVersion?.Minor ?? 0;

		public int VersionBuild => assemblyVersion?.Build ?? 0;

		public int VersionRevision => assemblyVersion?.Revision ?? 0;

		private readonly Version? assemblyVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
		public bool Equals(IPluginInfo? other) => Equals(this, other);
	}
}
