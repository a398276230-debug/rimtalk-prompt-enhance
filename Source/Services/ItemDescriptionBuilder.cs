using System.Linq;
using RimTalk.Service;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Helper class to build item descriptions with smart filtering
    /// </summary>
    public static class ItemDescriptionBuilder
    {
        /// <summary>
        /// Get item description based on settings and filters.
        /// Worldbuilder (ferny.Worldbuilder) description overrides take priority over def descriptions:
        /// players write concise, world-specific text there, while some apparel mods ship very long
        /// descriptions that hurt AI judgment. Label overrides are handled natively (Worldbuilder
        /// patches GenLabel.ThingLabel, which LabelCap goes through).
        /// </summary>
        public static string GetItemDescription(Thing item, int maxLength)
        {
            if (item?.def == null) return null;

            var settings = RimTalkHealthEnhanceMod.Settings;

            // Skip art description (Tynan tales) if enabled
            if (settings.SkipArtDescription)
            {
                // Check if item has art comp and tale ref (which means it has art description)
                CompArt artComp = item.TryGetComp<CompArt>();
                if (artComp != null && artComp.TaleRef != null)
                {
                    // It has art description, so we skip it to avoid "Tynan tales"
                    // But we still want the base description if available
                    // However, usually high quality items show art description instead of base description in game
                    // We want to force base description from def
                }
            }

            // Worldbuilder override first (player-written text beats mod def text)
            if (WorldbuilderBridge.TryGetDescriptionOverride(item, out string wbDesc))
                return CleanDescription(wbDesc, maxLength);

            // Always use def description to avoid art tales
            string desc = item.def.description;

            // If description is empty, try label
            if (string.IsNullOrEmpty(desc))
                return null;

            // Clean and truncate
            return CleanDescription(desc, maxLength);
        }
        
        /// <summary>
        /// Check if item should show description based on settings
        /// </summary>
        public static bool ShouldShowDescription(Thing item, PromptService.InfoLevel infoLevel)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            
            // Never show in Short mode
            if (infoLevel == PromptService.InfoLevel.Short)
                return false;
            
            // Quality filtering
            if (item.TryGetQuality(out QualityCategory quality))
            {
                if (quality < settings.MinQualityForDesc)
                    return false;
            }
            else
            {
                // Items without quality
                
                // Always show description for Buildings and Weapons even if they don't have quality
                // (unless they are common items which are filtered later)
                if (item.def.IsBuildingArtificial || item.def.IsWeapon)
                {
                    // Pass through to common item check
                }
                else
                {
                    // For other items (resources, etc.), respect the threshold
                    // Only show if threshold is Awful (meaning show everything)
                    if (settings.MinQualityForDesc > QualityCategory.Awful)
                        return false;
                }
            }
            
            // Skip common items
            if (settings.SkipCommonItems && IsCommonItem(item))
                return false;
            
            return true;
        }
        
        private static bool IsCommonItem(Thing item)
        {
            if (item.def == null) return true;

            // Raw resources, food, corpses, chunks, etc.
            return item.def.IsNutritionGivingIngestible ||
                   item.def.IsStuff ||
                   item.def.thingCategories?.Any(c => 
                       c.defName.Contains("Raw") || 
                       c.defName.Contains("Chunk") ||
                       c.defName.Contains("Corpses") ||
                       c.defName.Contains("StoneBlocks")) == true;
        }
        
        private static string CleanDescription(string desc, int maxLength)
        {
            if (string.IsNullOrEmpty(desc)) return "";

            // Replace newlines with spaces
            desc = desc.Replace("\n", " ").Replace("\r", "");
            
            // Remove multiple spaces
            desc = System.Text.RegularExpressions.Regex.Replace(desc, @"\s+", " ").Trim();
            
            // Truncate if too long
            if (desc.Length > maxLength)
            {
                // Try to cut at the last space before limit
                int cutIndex = desc.LastIndexOf(' ', maxLength - 3);
                if (cutIndex > 0)
                    desc = desc.Substring(0, cutIndex) + "...";
                else
                    desc = desc.Substring(0, maxLength - 3) + "...";
            }
            
            return desc;
        }

        /// <summary>
        /// Reflection bridge to Worldbuilder (ferny.Worldbuilder, optional dependency).
        /// Reads CustomizationData.descriptionOverride via CustomizationDataCollections.GetCustomizationData(Thing).
        /// Degrades to a no-op (single cached null check) when Worldbuilder is not installed.
        /// </summary>
        private static class WorldbuilderBridge
        {
            private static bool _resolved;
            private static System.Reflection.MethodInfo _getMethod;
            private static System.Reflection.FieldInfo _descriptionField;

            public static bool TryGetDescriptionOverride(Thing thing, out string description)
            {
                description = null;
                if (!_resolved) Resolve();
                if (_getMethod == null || _descriptionField == null) return false;

                try
                {
                    var data = _getMethod.Invoke(null, new object[] { thing });
                    if (data == null) return false;
                    description = _descriptionField.GetValue(data) as string;
                    return !string.IsNullOrEmpty(description);
                }
                catch
                {
                    // Worldbuilder internals changed or threw - disable the bridge for this session
                    _getMethod = null;
                    _descriptionField = null;
                    return false;
                }
            }

            private static void Resolve()
            {
                _resolved = true;
                try
                {
                    var collectionsType = HarmonyLib.AccessTools.TypeByName("Worldbuilder.CustomizationDataCollections");
                    if (collectionsType == null) return;

                    _getMethod = HarmonyLib.AccessTools.Method(collectionsType, "GetCustomizationData", new[] { typeof(Thing) });
                    if (_getMethod == null || _getMethod.ReturnType == null) return;

                    _descriptionField = HarmonyLib.AccessTools.Field(_getMethod.ReturnType, "descriptionOverride");
                }
                catch
                {
                    _getMethod = null;
                    _descriptionField = null;
                }
            }
        }
    }
}
