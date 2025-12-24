using System;
using HarmonyLib;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 监听地图变化，使布局缓存失效
    /// </summary>
    [HarmonyPatch]
    public static class LayoutCachePatch
    {
        // 监听房间形状变化
        [HarmonyPatch(typeof(Room), "Notify_RoomShapeChanged")]
        [HarmonyPostfix]
        public static void OnRoomShapeChanged(Room __instance)
        {
            if (__instance?.Map != null)
            {
                ColonyLayoutBuilder.InvalidateCache(__instance.Map);
            }
        }

        // 监听建筑拆除（可能影响房间或布局）
        [HarmonyPatch(typeof(Thing), "DeSpawn")]
        [HarmonyPostfix]
        public static void OnThingDeSpawn(Thing __instance)
        {
            if (__instance is Building && __instance.Map != null)
            {
                ColonyLayoutBuilder.InvalidateCache(__instance.Map);
            }
        }

        // 监听建筑建造
        [HarmonyPatch(typeof(Thing), "SpawnSetup")]
        [HarmonyPostfix]
        public static void OnThingSpawn(Thing __instance)
        {
            if (__instance is Building && __instance.Map != null)
            {
                ColonyLayoutBuilder.InvalidateCache(__instance.Map);
            }
        }
    }
}
