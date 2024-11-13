using Microsoft.Build.Framework;

namespace BepInEx.IL2CPP.MSBuild.Shared
{
    public class GameLibsPackage(ITaskItem taskItem)
    {
        public string Id { get; } = taskItem.ItemSpec;
        public string DummyDirectory { get; } = taskItem.GetMetadata("DummyDirectory");
        public string Version { get; } = taskItem.GetMetadata("Version");
        public string UnityVersion { get; } = taskItem.GetMetadata("UnityVersion");
    }
}
