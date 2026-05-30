using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace TessPaths
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log => Instance?.Logger;

        private Harmony _harmony;

        public ConfigEntry<bool> ModEnabled { get; private set; }
        public ConfigEntry<bool> UseWeightedAStar { get; private set; }
        public ConfigEntry<float> EvaTilePenalty { get; private set; }
        public ConfigEntry<float> DoorOpeningPenalty { get; private set; }
        public ConfigEntry<float> FireHazardPenalty { get; private set; }

        private void Awake()
        {
            Instance = this;

            ModEnabled = Config.Bind("General", "Enabled", true, "Should the mod functionality be enabled?");
            UseWeightedAStar = Config.Bind("Pathing", "UseWeightedAStar", true, "Toggle between Weighted A* (fastest path) and default JPS (shortest path).");
            EvaTilePenalty = Config.Bind("Pathing", "EvaTilePenalty", 2.0f, "Cost multiplier for moving in zero-G / EVA tiles. (Default 2.0x)");
            DoorOpeningPenalty = Config.Bind("Pathing", "DoorOpeningPenalty", 3.0f, "Extra cost penalty applied when a path goes through a closed door/portal.");
            FireHazardPenalty = Config.Bind("Pathing", "FireHazardPenalty", 100.0f, "High cost penalty to deter crew from walking through burning tiles unless forced.");

            if (!ModEnabled.Value)
            {
                Logger.LogInfo("TessPaths is disabled in the configuration.");
                return;
            }

            Logger.LogInfo($"TessPaths is loading (Version {PluginInfo.Version})...");

            _harmony = new Harmony(PluginInfo.GUID);
            _harmony.PatchAll();

            Logger.LogInfo("TessPaths loaded successfully!");
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
                Logger.LogInfo("TessPaths unpatched successfully.");
            }
        }
    }

    public static class PluginInfo
    {
        public const string GUID = "com.kevan.tesspaths";
        public const string Name = "TessPaths";
        public const string Version = "1.1.0";
    }
}
