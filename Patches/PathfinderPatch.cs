using HarmonyLib;
using Ostranauts.Pathing;
using UnityEngine;

namespace TessPaths.Patches
{
    [HarmonyPatch(typeof(Pathfinder), "Awake")]
    public static class PathfinderPatch
    {
        [HarmonyPostfix]
        public static void Postfix(Pathfinder __instance)
        {
            if (Plugin.Instance.UseWeightedAStar.Value)
            {
                Plugin.Log.LogInfo("TessPaths: Overriding default JumpPointSearch with WeightedAStarSearch...");
                var pathSearchProviderField = AccessTools.Field(typeof(Pathfinder), "_pathSearchProvider");
                if (pathSearchProviderField != null)
                {
                    pathSearchProviderField.SetValue(__instance, new WeightedAStarSearch());
                    Plugin.Log.LogInfo("TessPaths: Successfully replaced default Pathfinder search provider with WeightedAStarSearch.");
                }
                else
                {
                    Plugin.Log.LogError("TessPaths: Could not find _pathSearchProvider in Pathfinder!");
                }
            }
            else
            {
                Plugin.Log.LogInfo("TessPaths: Weighted A* pathing is disabled in configuration. Using default JPS.");
            }
        }
    }
}
