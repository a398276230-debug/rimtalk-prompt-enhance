using System.Linq;
using LudeonTK;
using RimTalkHealthEnhance.Services;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 开发者调试入口（GodMode/开发者模式可见）。
    /// 用途：
    /// 1. 绕过 UI 直接触发讨论链路，便于自动化测试（GABS execute_debug_action 可直接调用，无需点 FloatMenu）
    /// 2. 快速验证 RimTalk 集成（AddTalkRequest/AddUserHistory/Announcement 模式）
    /// 3. 快速重扫描事件类型池
    /// </summary>
    public static class DebugActions
    {
        private static ColonyAnnouncement FirstActiveAnnouncement()
        {
            var announcements = ColonyAnnouncementManager.Instance?.Data.Announcements;
            return announcements?.LastOrDefault(a => a.Status == AnnouncementStatus.Active)
                   ?? announcements?.LastOrDefault();
        }

        [DebugAction("RimTalk Enhance", "Test: Player discussion on last announcement", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestPlayerDiscussion()
        {
            var announcement = FirstActiveAnnouncement();
            if (announcement == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No announcement available. Capture an event first (dismiss a letter).");
                return;
            }

            var pawn = DiscussionService.SelectRandomColonist();
            if (pawn == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No eligible colonist (need awake, spawned, RimTalk-enabled).");
                return;
            }

            bool ok = DiscussionService.StartDiscussion(pawn, announcement);
            Log.Message($"[RimTalk Enhance][Debug] StartDiscussion -> {ok} (pawn={pawn.LabelShort}, item='{announcement.Title}')");
        }

        [DebugAction("RimTalk Enhance", "Test: Pawn announcement on last announcement", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestPawnAnnouncement()
        {
            var announcement = FirstActiveAnnouncement();
            if (announcement == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No announcement available. Capture an event first (dismiss a letter).");
                return;
            }

            var pawn = DiscussionService.SelectRandomColonist();
            if (pawn == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No eligible colonist (need awake, spawned, RimTalk-enabled).");
                return;
            }

            bool ok = DiscussionService.StartPawnSelfDiscussion(pawn, announcement);
            Log.Message($"[RimTalk Enhance][Debug] StartPawnSelfDiscussion -> {ok} (pawn={pawn.LabelShort}, item='{announcement.Title}')");
        }

        [DebugAction("RimTalk Enhance", "Test: Group discussion on last announcement", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestGroupDiscussion()
        {
            var announcement = FirstActiveAnnouncement();
            if (announcement == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No announcement available. Capture an event first (dismiss a letter).");
                return;
            }

            var colonists = GroupDiscussionService.GetAvailableColonists();
            if (colonists.Count == 0)
            {
                Log.Message("[RimTalk Enhance][Debug] No eligible colonists for group discussion.");
                return;
            }

            var leader = colonists[0];
            var participants = colonists.Take(4).ToList();

            bool ok = GroupDiscussionService.StartGroupDiscussion(announcement, leader, participants);
            Log.Message($"[RimTalk Enhance][Debug] StartGroupDiscussion -> {ok} (leader={leader.LabelShort}, participants={participants.Count}, item='{announcement.Title}')");
        }

        [DebugAction("RimTalk Enhance", "Test: Group discussion (leader only, no pre-selected participants)", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestGroupDiscussionLeaderOnly()
        {
            var announcement = FirstActiveAnnouncement();
            if (announcement == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No announcement available. Capture an event first (dismiss a letter).");
                return;
            }

            var colonists = GroupDiscussionService.GetAvailableColonists();
            if (colonists.Count == 0)
            {
                Log.Message("[RimTalk Enhance][Debug] No eligible colonists for group discussion.");
                return;
            }

            var leader = colonists[0];
            bool ok = GroupDiscussionService.StartGroupDiscussion(announcement, leader, new System.Collections.Generic.List<Pawn>());
            Log.Message($"[RimTalk Enhance][Debug] StartGroupDiscussion(leaderOnly) -> {ok} (leader={leader.LabelShort}, preselected=0, item='{announcement.Title}')");
        }

        [DebugAction("RimTalk Enhance", "Test: Worldbuilder description override bridge", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestWorldbuilderBridge()
        {
            var pawn = Find.CurrentMap?.mapPawns.FreeColonistsSpawned.FirstOrDefault();
            var target = (Verse.Thing)pawn?.equipment?.Primary ?? pawn?.apparel?.WornApparel?.FirstOrDefault();
            if (pawn == null || target == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No colonist with equipped weapon/apparel found.");
                return;
            }

            string before = ItemDescriptionBuilder.GetItemDescription(target, int.MaxValue);
            Log.Message($"[RimTalk Enhance][Debug] WB bridge BEFORE override: '{(before ?? "(null)")}'");

            // 反射写入 Worldbuilder CustomizationData.descriptionOverride
            var collectionsType = HarmonyLib.AccessTools.TypeByName("Worldbuilder.CustomizationDataCollections");
            if (collectionsType == null)
            {
                Log.Message("[RimTalk Enhance][Debug] Worldbuilder not installed - bridge degrades to no-op (expected without the mod).");
                return;
            }

            var data = HarmonyLib.AccessTools.Method(collectionsType, "GetCustomizationData", new[] { typeof(Verse.Thing) })?.Invoke(null, new object[] { target });
            if (data == null)
            {
                // 无现有数据：创建并通过 thingCustomizationData 字典注册
                var ctor = HarmonyLib.AccessTools.Constructor(HarmonyLib.AccessTools.TypeByName("Worldbuilder.CustomizationData"));
                data = ctor?.Invoke(null);
                var dict = HarmonyLib.AccessTools.Field(collectionsType, "thingCustomizationData")?.GetValue(null) as System.Collections.IDictionary;
                dict?[target] = data;
            }
            if (data == null)
            {
                Log.Message("[RimTalk Enhance][Debug] Failed to create Worldbuilder CustomizationData.");
                return;
            }

            HarmonyLib.AccessTools.Field(data.GetType(), "descriptionOverride")?.SetValue(data, "WB测试描述：这把武器由Worldbuilder玩家自定义。");
            Log.Message("[RimTalk Enhance][Debug] WB override injected.");

            string after = ItemDescriptionBuilder.GetItemDescription(target, int.MaxValue);
            Log.Message($"[RimTalk Enhance][Debug] WB bridge AFTER override: '{after}'");
            Log.Message(after != null && after.Contains("WB测试描述")
                ? "[RimTalk Enhance][Debug] WB BRIDGE OK: Worldbuilder override takes priority."
                : "[RimTalk Enhance][Debug] WB BRIDGE FAIL: override not picked up.");
        }

        [DebugAction("RimTalk Enhance", "Test: Open group discussion dialog", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestOpenGroupDiscussionDialog()
        {
            var announcement = FirstActiveAnnouncement();
            if (announcement == null)
            {
                Log.Message("[RimTalk Enhance][Debug] No announcement available. Capture an event first (dismiss a letter).");
                return;
            }

            Find.WindowStack.Add(new UI.GroupDiscussionDialog(announcement));
            Log.Message($"[RimTalk Enhance][Debug] Group discussion dialog opened for '{announcement.Title}'.");
        }

        [DebugAction("RimTalk Enhance", "Test: Rescan archivable types", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestRescanTypes()
        {
            ArchivableTypeScanner.Scan();
            Log.Message($"[RimTalk Enhance][Debug] Rescan complete: {AutoCaptureSettings.DiscoveredEventTypes.Count} types, filter entries={RimTalkHealthEnhanceMod.Settings.EnabledEventTypes.Count}.");
        }

        [DebugAction("RimTalk Enhance", "Toggle debug logging", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ToggleDebugLogging()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            settings.Misc.DebugMode = !settings.Misc.DebugMode;
            settings.Write();
            Log.Message($"[RimTalk Enhance][Debug] Debug logging is now {(settings.Misc.DebugMode ? "ON" : "OFF")}.");
        }

        [DebugAction("RimTalk Enhance", "Test: Dump colonist full context", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void TestDumpColonistContext()
        {
            // 临时强制开启 debug 输出（不修改玩家设置，仅本次 dump 生效）
            var settings = RimTalkHealthEnhanceMod.Settings;
            bool prev = settings.Misc.DebugMode;
            settings.Misc.DebugMode = true;
            try
            {
                var pawn = Find.CurrentMap?.mapPawns.FreeColonistsSpawned.FirstOrDefault();
                if (pawn == null)
                {
                    Log.Message("[RimTalk Enhance][Debug] No spawned colonist found.");
                    return;
                }
                DebugHelper.LogPawnContext(pawn);
            }
            finally
            {
                settings.Misc.DebugMode = prev;
            }
        }
    }
}

