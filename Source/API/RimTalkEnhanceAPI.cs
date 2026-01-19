using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimTalk;
using RimTalk.API;
using RimTalk.Prompt;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// RimTalk 增强提示词 API 注册服务
    /// 使用 RimTalk 官方 API 注册 hooks、变量和 entries
    /// </summary>
    public static class RimTalkEnhanceAPI
    {
        private const string MOD_ID = "RimTalkHealthEnhance";
        private static bool _registered = false;
        private static string _colonyStatusEntryId = null;

        /// <summary>
        /// 在 Mod 初始化时调用，注册所有 API hooks
        /// </summary>
        public static void Initialize()
        {
            if (_registered) return;

            try
            {
                RegisterPawnHooks();
                RegisterContextVariables();
                RegisterPromptEntries();

                _registered = true;
                Log.Message("[RimTalk Enhance] API hooks registered successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to register API hooks: {ex}");
                // 如果 API 注册失败，Harmony patches 仍然作为备用方案
            }
        }

        /// <summary>
        /// Mod 卸载时调用，清理所有注册
        /// </summary>
        public static void Cleanup()
        {
            if (!_registered) return;

            try
            {
                RimTalkPromptAPI.UnregisterAllHooks(MOD_ID);

                // 移除 Prompt Entry
                if (!string.IsNullOrEmpty(_colonyStatusEntryId))
                {
                    RimTalkPromptAPI.RemovePromptEntry(_colonyStatusEntryId);
                }

                _registered = false;
                Log.Message("[RimTalk Enhance] API hooks unregistered");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhance] Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查 API 是否已注册
        /// </summary>
        public static bool IsRegistered => _registered;

        #region Pawn Hooks

        private static void RegisterPawnHooks()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;

            // Health Hook (Override) - 增强健康信息
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Health,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeHealth) return null;

                    // 使用 Normal 作为默认 InfoLevel（与 RimTalk 主对话一致）
                    var infoLevel = PromptService.InfoLevel.Normal;
                    return HealthInfoBuilder.BuildEnhancedHealthContext(pawn, infoLevel);
                },
                priority: 50 // 较低优先级，优先执行
            );

            // Equipment Hook (Override) - 增强装备信息
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Equipment,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeEquipment) return null;

                    var infoLevel = PromptService.InfoLevel.Normal;
                    return BuildEnhancedEquipmentContext(pawn, infoLevel);
                },
                priority: 50
            );

            // Relations Hook (Override) - 解除关系数量限制
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Social,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeRelations) return original;

                    // 如果未启用无限关系，返回原始值
                    if (!settings.UnlimitedRelations) return original;

                    return GetRelationsStringUnlimited(pawn);
                },
                priority: 50
            );

            // Traits Hook (Override) - 解除特质数量限制
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Traits,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeTraits) return original;

                    // 如果未启用无限特质，返回原始值
                    if (!settings.UnlimitedTraits) return original;

                    var infoLevel = PromptService.InfoLevel.Normal;
                    return GetTraitsContextUnlimited(pawn, infoLevel);
                },
                priority: 50
            );

            // Location Hook (Append) - 追加相对位置信息
            // 现在 Location 是 Pawn 类型的 category，可以直接获取 pawn
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Location,
                ContextHookRegistry.HookOperation.Append,
                (pawn, original) =>
                {
                    if (!settings.ShowRelativeLocation) return original;
                    if (pawn?.Map == null) return original;

                    string relativeLocation = LocationContextBuilder.GetRelativeLocation(pawn);
                    if (!string.IsNullOrEmpty(relativeLocation))
                    {
                        string prefix = pawn.Map.IsPlayerHome
                            ? "Relative Position"
                            : "Current Map";
                        return original + $"\n{prefix}: {relativeLocation}";
                    }
                    return original;
                },
                priority: 50
            );
        }

        #endregion

        #region Context Variables

        private static void RegisterContextVariables()
        {
            // 注册殖民地状况变量 {{colony_status}}
            RimTalkPromptAPI.RegisterContextVariable(
                MOD_ID,
                "colony_status",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.ShowColonyAnnouncements) return "";

                    return AnnouncementBuilder.BuildAnnouncementContext() ?? "";
                },
                description: "RTE_API_ColonyStatus_Desc".Translate(),
                priority: 100
            );

            // 注册殖民地历史变量 {{colony_history}}
            RimTalkPromptAPI.RegisterContextVariable(
                MOD_ID,
                "colony_history",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.EnableAISynthesis || !settings.InjectSnapshotToContext)
                        return "";

                    return BuildColonyHistoryContext() ?? "";
                },
                description: "RTE_API_ColonyHistory_Desc".Translate(),
                priority: 100
            );

            // 注册殖民地布局变量 {{colony_layout}}
            RimTalkPromptAPI.RegisterContextVariable(
                MOD_ID,
                "colony_layout",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.EnableGlobalLayout) return "";

                    var map = Find.CurrentMap;
                    if (map == null || !map.IsPlayerHome) return "";

                    return ColonyLayoutBuilder.GetColonyLayout(map) ?? "";
                },
                description: "RTE_API_ColonyLayout_Desc".Translate(),
                priority: 100
            );

            // 注册派系信息变量 {{colony_factions}}
            RimTalkPromptAPI.RegisterContextVariable(
                MOD_ID,
                "colony_factions",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.ShowFactionRelations) return "";

                    return FactionInfoBuilder.BuildFactionContext() ?? "";
                },
                description: "RTE_API_ColonyFactions_Desc".Translate(),
                priority: 100
            );
        }

        #endregion

        #region Prompt Entries

        /// <summary>
        /// 获取确定性的 Entry ID（用于去重和移除）
        /// 格式：mod_{sanitizedModId}_{sanitizedName}
        /// </summary>
        private static string GetDeterministicEntryId(string modId, string name)
        {
            var sanitizedModId = SanitizeForId(modId);
            var sanitizedName = SanitizeForId(name);
            return $"mod_{sanitizedModId}_{sanitizedName}";
        }
        
        private static string SanitizeForId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "unknown";
            return System.Text.RegularExpressions.Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]", "");
        }

        private const string COLONY_STATUS_ENTRY_NAME = "Colony Status (RimTalk Enhance)";

        /// <summary>
        /// 获取当前 Entry 的内容（用于更新）
        /// </summary>
        private static string GetColonyStatusEntryContent()
        {
            return "{{colony_status}}\n{{colony_history}}\n{{colony_layout}}\n{{colony_factions}}";
        }

        private static void RegisterPromptEntries()
        {
            // 预先计算确定性 ID（用于清理时移除和更新时查找）
            _colonyStatusEntryId = GetDeterministicEntryId(MOD_ID, COLONY_STATUS_ENTRY_NAME);
            
            // ⭐ 首先检查是否已存在该条目，如果存在则更新内容（确保 Mod 更新后内容同步到玩家预设）
            try
            {
                var preset = RimTalkPromptAPI.GetActivePreset();
                if (preset != null)
                {
                    var existingEntry = preset.GetEntry(_colonyStatusEntryId);
                    if (existingEntry != null)
                    {
                        // ⭐ 条目已存在 → 直接更新 Content
                        existingEntry.Content = GetColonyStatusEntryContent();
                        Log.Message($"[RimTalk Enhance] ✓ Updated existing PromptEntry: {COLONY_STATUS_ENTRY_NAME}");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Message($"[RimTalk Enhance] Failed to check/update existing entry: {ex.Message}");
            }
            
            // 创建殖民地状况 Entry，包含所有胡子变量
            // 注意：PromptEntry 会根据 SourceModId 和 Name 自动生成确定性 ID
            // 这样即使 Mod 重新加载也不会重复添加
            var entry = RimTalkPromptAPI.CreatePromptEntry(
                name: COLONY_STATUS_ENTRY_NAME,
                content: GetColonyStatusEntryContent(),
                role: PromptRole.System,
                position: PromptPosition.Relative,
                inChatDepth: 0,
                sourceModId: MOD_ID
            );

            // 插入到 "Chat History" 之前（即 "Pawn Profiles" 之后，第四位）
            // 如果找不到 "Chat History"，则回退到添加到末尾
            if (RimTalkPromptAPI.InsertPromptEntryBeforeName(entry, "Chat History"))
            {
                Log.Message("[RimTalk Enhance] Colony status entry inserted before Chat History");
            }
            else
            {
                Log.Message("[RimTalk Enhance] Chat History not found, colony status entry added at end");
            }
        }

        #endregion

        #region Helper Methods - Equipment

        /// <summary>
        /// 构建增强装备上下文（复制自 EquipmentContextPatch）
        /// </summary>
        private static string BuildEnhancedEquipmentContext(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var parts = new List<string>();
            int descCount = 0;

            // === 1. Weapon ===
            if (pawn.equipment?.Primary != null)
            {
                var weapon = pawn.equipment.Primary;
                string weaponInfo = $"Weapon: {weapon.LabelCap}";

                if (settings.ShowEquipmentDesc &&
                    descCount < settings.MaxItemsWithDesc &&
                    ItemDescriptionBuilder.ShouldShowDescription(weapon, infoLevel))
                {
                    string desc = ItemDescriptionBuilder.GetItemDescription(weapon, settings.ItemMaxDescriptionLength);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        weaponInfo += $" - {desc}";
                        descCount++;
                    }
                }

                parts.Add(weaponInfo);
            }

            // === 2. Apparel ===
            var apparels = pawn.apparel?.WornApparel;
            if (apparels?.Any() == true)
            {
                var apparelInfos = new List<string>();

                foreach (var apparel in apparels)
                {
                    string info = apparel.LabelCap;

                    if (settings.ShowEquipmentDesc &&
                        descCount < settings.MaxItemsWithDesc &&
                        ItemDescriptionBuilder.ShouldShowDescription(apparel, infoLevel))
                    {
                        string desc = ItemDescriptionBuilder.GetItemDescription(apparel, settings.ItemMaxDescriptionLength);
                        if (!string.IsNullOrEmpty(desc))
                        {
                            info += $" - {desc}";
                            descCount++;
                        }
                    }

                    apparelInfos.Add(info);
                }

                parts.Add($"Apparel: {string.Join(", ", apparelInfos)}");
            }

            // === 3. Carried Item ===
            if (pawn.carryTracker?.CarriedThing != null)
            {
                var carried = pawn.carryTracker.CarriedThing;
                string carriedInfo = $"Carrying: {carried.LabelCap}";

                if (settings.ShowCarriedItemDesc &&
                    descCount < settings.MaxItemsWithDesc &&
                    ItemDescriptionBuilder.ShouldShowDescription(carried, infoLevel))
                {
                    string desc = ItemDescriptionBuilder.GetItemDescription(carried, settings.ItemMaxDescriptionLength);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        carriedInfo += $" - {desc}";
                        descCount++;
                    }
                }

                parts.Add(carriedInfo);
            }

            // === 4. Inventory Items ===
            if (settings.ShowInventoryItems && pawn.inventory?.innerContainer != null)
            {
                var inventoryItems = pawn.inventory.innerContainer
                    .Take(settings.MaxInventoryItems)
                    .ToList();

                if (inventoryItems.Any())
                {
                    var inventoryInfos = new List<string>();

                    foreach (var item in inventoryItems)
                    {
                        string info = $"{item.LabelCap} x{item.stackCount}";

                        if (settings.ShowInventoryDesc &&
                            descCount < settings.MaxItemsWithDesc &&
                            ItemDescriptionBuilder.ShouldShowDescription(item, infoLevel))
                        {
                            string desc = ItemDescriptionBuilder.GetItemDescription(item, settings.ItemMaxDescriptionLength);
                            if (!string.IsNullOrEmpty(desc))
                            {
                                info += $" - {desc}";
                                descCount++;
                            }
                        }

                        inventoryInfos.Add(info);
                    }

                    parts.Add($"Inventory: {string.Join(", ", inventoryInfos)}");
                }
            }

            // === 5. Interaction Item/Building ===
            if (settings.ShowInteractionDesc && pawn.CurJob?.targetA.Thing != null)
            {
                var target = pawn.CurJob.targetA.Thing;

                bool shouldShow = true;
                if (settings.OnlyShowImportantBuildings)
                {
                    if (target.def.category != ThingCategory.Building &&
                        target.def.category != ThingCategory.Item &&
                        target.def.category != ThingCategory.Pawn)
                        shouldShow = false;

                    if (string.IsNullOrEmpty(target.def.description))
                        shouldShow = false;
                }

                if (shouldShow && ItemDescriptionBuilder.ShouldShowDescription(target, infoLevel))
                {
                    string desc = ItemDescriptionBuilder.GetItemDescription(target, settings.InteractionMaxDescLength);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        string action = "Interacting with";
                        if (pawn.CurJob.def.driverClass != null)
                        {
                            string driverName = pawn.CurJob.def.driverClass.Name;
                            if (driverName.Contains("Research")) action = "Researching at";
                            else if (driverName.Contains("Ingest")) action = "Consuming";
                            else if (driverName.Contains("Sleep") || driverName.Contains("Lay")) action = "Sleeping/Resting on";
                            else if (driverName.Contains("Work") || driverName.Contains("DoBill")) action = "Working at";
                            else if (driverName.Contains("Play") || driverName.Contains("Joy")) action = "Playing at";
                            else if (driverName.Contains("Repair")) action = "Repairing";
                            else if (driverName.Contains("Construct")) action = "Constructing";
                        }

                        parts.Add($"Activity: {action} {target.LabelCap} - {desc}");
                    }
                }
            }

            if (parts.Any())
                return $"Equipment: {string.Join("\n", parts)}";

            return null;
        }

        #endregion

        #region Helper Methods - Relations

        private const float FriendOpinionThreshold = 20f;
        private const float RivalOpinionThreshold = -20f;

        /// <summary>
        /// 获取无限制的关系字符串（复制自 RelationsContextPatch）
        /// </summary>
        private static string GetRelationsStringUnlimited(Pawn pawn)
        {
            if (pawn?.relations == null) return "";

            StringBuilder relationsSb = new StringBuilder();
            HashSet<Pawn> processedPawns = new HashSet<Pawn>();

            // Step 1: Process all DirectRelations
            if (pawn.relations.DirectRelations != null)
            {
                foreach (var relation in pawn.relations.DirectRelations)
                {
                    Pawn otherPawn = relation.otherPawn;
                    if (ShouldProcessPawn(pawn, otherPawn))
                    {
                        processedPawns.Add(otherPawn);
                        AppendRelationInfo(pawn, otherPawn, relationsSb);
                    }
                }
            }

            // Step 2: Process all colony pawns for opinion-based relationships
            var allColonyPawns = Find.CurrentMap?.mapPawns?.AllPawnsSpawned;
            if (allColonyPawns != null)
            {
                foreach (Pawn otherPawn in allColonyPawns)
                {
                    if (!processedPawns.Contains(otherPawn) && ShouldProcessPawn(pawn, otherPawn))
                    {
                        processedPawns.Add(otherPawn);
                        AppendRelationInfo(pawn, otherPawn, relationsSb);
                    }
                }
            }

            if (relationsSb.Length > 0)
            {
                relationsSb.Length -= 2; // Remove trailing ", "
                return "Relations: " + relationsSb;
            }

            return "";
        }

        private static bool ShouldProcessPawn(Pawn pawn, Pawn otherPawn)
        {
            if (otherPawn == null || otherPawn == pawn) return false;
            if (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) return false;
            if (otherPawn.Dead) return false;
            if (otherPawn.relations is { hidePawnRelations: true }) return false;
            return true;
        }

        private static void AppendRelationInfo(Pawn pawn, Pawn otherPawn, StringBuilder sb)
        {
            try
            {
                float opinionValue = pawn.relations.OpinionOf(otherPawn);
                string label = null;

                PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(otherPawn);
                if (mostImportantRelation != null)
                {
                    label = mostImportantRelation.GetGenderSpecificLabelCap(otherPawn);
                }

                if (string.IsNullOrEmpty(label))
                {
                    label = GetStatusLabel(pawn, otherPawn);
                }

                if (string.IsNullOrEmpty(label) && !pawn.IsVisitor() && !pawn.IsEnemy())
                {
                    if (opinionValue >= FriendOpinionThreshold)
                        label = "Friend".Translate();
                    else if (opinionValue <= RivalOpinionThreshold)
                        label = "Rival".Translate();
                    else
                        label = "Acquaintance".Translate();
                }

                if (!string.IsNullOrEmpty(label))
                {
                    string pawnName = otherPawn.LabelShort;
                    string opinion = opinionValue.ToStringWithSign();
                    sb.Append($"{pawnName}({label}) {opinion}, ");
                }
            }
            catch (Exception)
            {
                // Skip this pawn if opinion calculation fails
            }
        }

        private static string GetStatusLabel(Pawn pawn, Pawn otherPawn)
        {
            if ((pawn.IsPrisoner || pawn.IsSlave) && otherPawn.IsFreeNonSlaveColonist)
                return "Master".Translate();

            if (otherPawn.IsPrisoner) return "Prisoner".Translate();
            if (otherPawn.IsSlave) return "Slave".Translate();

            if (pawn.Faction != null && otherPawn.Faction != null && pawn.Faction.HostileTo(otherPawn.Faction))
                return "Enemy".Translate();

            return null;
        }

        #endregion

        #region Helper Methods - Traits

        /// <summary>
        /// 获取无限制的特质字符串（复制自 TraitsContextPatch）
        /// </summary>
        private static string GetTraitsContextUnlimited(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var traits = new List<string>();
            foreach (var trait in pawn.story?.traits?.TraitsSorted ?? Enumerable.Empty<Trait>())
            {
                var degreeData = trait.def.degreeDatas?.FirstOrDefault(d => d.degree == trait.Degree);
                if (degreeData != null)
                {
                    var traitText = infoLevel == PromptService.InfoLevel.Full
                        ? $"{degreeData.label}:{RimTalk.Util.CommonUtil.Sanitize(degreeData.description, pawn)}"
                        : degreeData.label;
                    traits.Add(traitText);
                }
            }

            if (traits.Any())
            {
                var separator = infoLevel == PromptService.InfoLevel.Full ? "\n" : ",";
                return $"Traits: {string.Join(separator, traits)}";
            }
            return null;
        }

        #endregion

        #region Helper Methods - Colony History

        /// <summary>
        /// 构建殖民地历史快照上下文（用于 {{colony_history}} 变量）
        /// 使用 Markdown 格式
        /// </summary>
        private static string BuildColonyHistoryContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var manager = Current.Game?.GetComponent<ColonyAnnouncementManager>();

            if (manager?.Data?.DailySnapshots == null || manager.Data.DailySnapshots.Count == 0)
                return null;

            var snapshotsWithSummary = manager.Data.DailySnapshots
                .Where(s => !string.IsNullOrEmpty(s.AISummary))
                .OrderByDescending(s => s.AbsTick)
                .ToList();

            if (snapshotsWithSummary.Count == 0)
                return null;

            long maxAbsTick = snapshotsWithSummary.Max(s => s.AbsTick);
            long ticksToInject = (long)(settings.SnapshotInjectDays * GenDate.TicksPerDay);

            var recentSnapshots = snapshotsWithSummary
                .Where(s => maxAbsTick - s.AbsTick < ticksToInject)
                .ToList();

            if (!recentSnapshots.Any())
                return null;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("## Recent Colony History");

            foreach (var snapshot in recentSnapshots)
            {
                string gameDateStr = snapshot.GetDateStringWithOffset(manager.Data.DisplayTickOffset, Vector2.zero);
                sb.AppendLine($"[{gameDateStr}] {snapshot.AISummary}");
            }

            return sb.ToString().TrimEnd();
        }

        #endregion
    }
}