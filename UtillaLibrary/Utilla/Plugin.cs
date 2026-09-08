using BepInEx;
using BepInEx.Logging;
using GorillaLibrary;
using System;
using System.Linq;
using Utilla.Models;

namespace Utilla
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    [BepInDependency("dev.gorillalibrary", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        static Plugin()
        {
            AssemblyRedirect.Install();
        }

        public void Awake()
        {
            Log = Logger;

            AssemblyRedirect.Install();

            CompatBridge.RoomJoined += OnRoomJoined;
            CompatBridge.RoomLeft += OnRoomLeft;

            GorillaLibrary.Events.Core.OnGameInitialized += OnGameInitialized;

            Log.LogInfo("Utilla compatibility layer ready, backed by GorillaLibrary");
        }

        public void OnDestroy()
        {
            CompatBridge.RoomJoined -= OnRoomJoined;
            CompatBridge.RoomLeft -= OnRoomLeft;

            GorillaLibrary.Events.Core.OnGameInitialized -= OnGameInitialized;
        }

        private void OnRoomJoined(bool isPrivate, string gamemode)
        {
            Events.Instance.TriggerRoomJoin(new Events.RoomJoinedArgs
            {
                isPrivate = isPrivate,
                Gamemode = gamemode
            });
        }

        private void OnRoomLeft(bool isPrivate, string gamemode)
        {
            Events.Instance.TriggerRoomLeft(new Events.RoomJoinedArgs
            {
                isPrivate = isPrivate,
                Gamemode = gamemode
            });
        }

        private void OnGameInitialized() => Events.Instance.TriggerGameInitialized();
    }

    public class PluginInfo
    {
        public const string Name = "Utilla";
        public const string GUID = "org.legoandmars.gorillatag.utilla";
        public const string Version = "1.7.0";
        public const string VersionURL = "https://github.com/sirkingbinx/Utilla/blob/master/Version.txt?raw=true";

        public BaseUnityPlugin Plugin { get; set; }
        public Gamemode[] Gamemodes { get; set; }
        public Action<string> OnGamemodeJoin { get; set; }
        public Action<string> OnGamemodeLeave { get; set; }

        public override string ToString()
        {
            return $"{Plugin.Info.Metadata.Name} [{string.Join(", ", Gamemodes.Select(x => x.DisplayName))}]";
        }
    }
}
