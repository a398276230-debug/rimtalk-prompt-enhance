using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTalk;
using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Harmony patch to replace RimTalk's GetEquipmentContext method with enhanced version
    /// </summary>
    [HarmonyPatch(typeof(ContextBuilder), "GetEquipmentContext")]
    public static class EquipmentContextPatch
    {
        /// <summary>
        /// Prefix patch that replaces the original method entirely
        /// </summary>
        static bool Prefix(Pawn pawn, PromptService.InfoLevel infoLevel, ref string __result)
        {
            // Check if equipment context is enabled in RimTalk settings
            var contextSettings = Settings.Get().Context;
            if (!contextSettings.IncludeEquipment)
            {
                __result = null;
                return false; // Skip original method
            }

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

            // === 3. Carried Item (New) ===
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

            // === 4. Inventory Items (New, Optional) ===
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

            // === 5. Interaction Item/Building (New) ===
            if (settings.ShowInteractionDesc && pawn.CurJob?.targetA.Thing != null)
            {
                var target = pawn.CurJob.targetA.Thing;
                
                // Filter logic
                bool shouldShow = true;
                if (settings.OnlyShowImportantBuildings)
                {
                    // Only show buildings/items that are likely to be interactive
                    // Skip walls, floors, filth, motes, etc.
                    if (target.def.category != ThingCategory.Building && 
                        target.def.category != ThingCategory.Item &&
                        target.def.category != ThingCategory.Pawn) // Interaction with pawns is handled by relations/social
                        shouldShow = false;
                        
                    // Skip things without description
                    if (string.IsNullOrEmpty(target.def.description))
                        shouldShow = false;
                }
                
                if (shouldShow && ItemDescriptionBuilder.ShouldShowDescription(target, infoLevel))
                {
                    string desc = ItemDescriptionBuilder.GetItemDescription(target, settings.InteractionMaxDescLength);
                    if (!string.IsNullOrEmpty(desc))
                    {
                        // Try to get a meaningful action name
                        string action = "Interacting with";
                        if (pawn.CurJob.def.driverClass != null)
                        {
                            // Map common job drivers to actions
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
                __result = $"Equipment: {string.Join("\n", parts)}";
            else
                __result = null;
            
            // Debug logging in dev mode
            if (Prefs.DevMode && !string.IsNullOrEmpty(__result))
            {
                Log.Message($"[RimTalk增强] {pawn.LabelShort} 的装备信息:\n{__result}");
            }

            return false; // Skip original method
        }
    }
}
