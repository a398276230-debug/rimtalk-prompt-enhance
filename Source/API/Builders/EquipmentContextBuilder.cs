using System.Collections.Generic;
using System.Linq;
using RimTalk.Service;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.API
{
    /// <summary>
    /// 构建增强装备上下文信息
    /// </summary>
    internal static class EquipmentContextBuilder
    {
        /// <summary>
        /// 构建增强装备上下文
        /// </summary>
        public static string Build(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            var parts = new List<string>();
            int descCount = 0;

            // === 1. Weapon ===
            BuildWeaponInfo(pawn, settings, infoLevel, parts, ref descCount);

            // === 2. Apparel ===
            BuildApparelInfo(pawn, settings, infoLevel, parts, ref descCount);

            // === 3. Carried Item ===
            BuildCarriedItemInfo(pawn, settings, infoLevel, parts, ref descCount);

            // === 4. Inventory Items ===
            BuildInventoryInfo(pawn, settings, infoLevel, parts, ref descCount);

            // === 5. Interaction Item/Building ===
            BuildInteractionInfo(pawn, settings, infoLevel, parts, ref descCount);

            if (parts.Any())
                return $"Equipment: {string.Join("\n", parts)}";

            return null;
        }

        private static void BuildWeaponInfo(Pawn pawn, HealthEnhanceSettings settings,
            PromptService.InfoLevel infoLevel, List<string> parts, ref int descCount)
        {
            if (pawn.equipment?.Primary == null) return;

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

        private static void BuildApparelInfo(Pawn pawn, HealthEnhanceSettings settings,
            PromptService.InfoLevel infoLevel, List<string> parts, ref int descCount)
        {
            var apparels = pawn.apparel?.WornApparel;
            if (apparels?.Any() != true) return;

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

        private static void BuildCarriedItemInfo(Pawn pawn, HealthEnhanceSettings settings,
            PromptService.InfoLevel infoLevel, List<string> parts, ref int descCount)
        {
            if (pawn.carryTracker?.CarriedThing == null) return;

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

        private static void BuildInventoryInfo(Pawn pawn, HealthEnhanceSettings settings,
            PromptService.InfoLevel infoLevel, List<string> parts, ref int descCount)
        {
            if (!settings.ShowInventoryItems || pawn.inventory?.innerContainer == null) return;

            var inventoryItems = pawn.inventory.innerContainer
                .Take(settings.MaxInventoryItems)
                .ToList();

            if (!inventoryItems.Any()) return;

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

        private static void BuildInteractionInfo(Pawn pawn, HealthEnhanceSettings settings,
            PromptService.InfoLevel infoLevel, List<string> parts, ref int descCount)
        {
            if (!settings.ShowInteractionDesc || pawn.CurJob?.targetA.Thing == null) return;

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

            if (!shouldShow || !ItemDescriptionBuilder.ShouldShowDescription(target, infoLevel)) return;

            string desc = ItemDescriptionBuilder.GetItemDescription(target, settings.InteractionMaxDescLength);
            if (string.IsNullOrEmpty(desc)) return;

            string action = GetInteractionAction(pawn);
            parts.Add($"Activity: {action} {target.LabelCap} - {desc}");
        }

        private static string GetInteractionAction(Pawn pawn)
        {
            if (pawn.CurJob.def.driverClass == null) return "Interacting with";

            string driverName = pawn.CurJob.def.driverClass.Name;

            if (driverName.Contains("Research")) return "Researching at";
            if (driverName.Contains("Ingest")) return "Consuming";
            if (driverName.Contains("Sleep") || driverName.Contains("Lay")) return "Sleeping/Resting on";
            if (driverName.Contains("Work") || driverName.Contains("DoBill")) return "Working at";
            if (driverName.Contains("Play") || driverName.Contains("Joy")) return "Playing at";
            if (driverName.Contains("Repair")) return "Repairing";
            if (driverName.Contains("Construct")) return "Constructing";

            return "Interacting with";
        }
    }
}