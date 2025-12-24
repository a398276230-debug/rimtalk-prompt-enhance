using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Main mod class that initializes Harmony patches and provides settings UI
    /// </summary>
    public class RimTalkHealthEnhanceMod : Mod
    {
        public static HealthEnhanceSettings Settings;
        
        private static int _tabIndex = 0;
        private static bool _archivableTypesScanned = false;

        public RimTalkHealthEnhanceMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HealthEnhanceSettings>();
            
            var harmony = new Harmony("ruaji.rimtalkpromptenhance");
            harmony.PatchAll();
            
            Log.Message("[RimTalk Enhanced Prompt] Harmony patches applied successfully.");
        }

        public override string SettingsCategory() => "RimTalk 增强提示词";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Scan for types if opening the events tab or first time
            if (!_archivableTypesScanned)
            {
                ScanArchivableTypes();
            }

            // Tabs
            Rect tabRect = inRect;
            tabRect.y += 10f; // 稍微下移，避免与顶部标题过于紧凑
            tabRect.height = 30f;
            
            Rect contentRect = inRect;
            contentRect.yMin += 45f; // 增加间距

            Widgets.DrawMenuSection(contentRect);
            contentRect = contentRect.ContractedBy(10f);

            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("RTE_Settings_Tab_ContextEnhancement".Translate(), () => _tabIndex = 0, _tabIndex == 0),
                new TabRecord("RTE_Settings_Tab_ColonyStatus".Translate(), () => _tabIndex = 1, _tabIndex == 1)
            };
            
            TabDrawer.DrawTabs(tabRect, tabs);

            if (_tabIndex == 0)
            {
                Settings.DoContextEnhancementWindowContents(contentRect);
            }
            else
            {
                Settings.DoColonyStatusWindowContents(contentRect);
            }
            
            base.DoSettingsWindowContents(inRect);
        }

        private void ScanArchivableTypes()
        {
            if (_archivableTypesScanned) return;

            try
            {
                var types = new HashSet<string>(HealthEnhanceSettings.DiscoveredEventTypes);
                bool changed = false;

                // 1. Scan all assemblies for IArchivable implementations
                foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var archivableTypes = assembly.GetTypes()
                            .Where(t => typeof(IArchivable).IsAssignableFrom(t) &&
                                        !t.IsInterface &&
                                        !t.IsAbstract)
                            .Select(t => t.FullName);

                        foreach (var type in archivableTypes)
                        {
                            if (types.Add(type)) changed = true;
                        }
                    }
                    catch (System.Exception)
                    {
                        // Ignore assembly load errors
                    }
                }

                // 2. Also scan current archive if game is loaded (just in case)
                if (Current.Game != null && Find.Archive != null)
                {
                    foreach (var archivable in Find.Archive.ArchivablesListForReading)
                    {
                        string typeName = archivable.GetType().FullName;
                        if (types.Add(typeName))
                        {
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    HealthEnhanceSettings.DiscoveredEventTypes = types.OrderBy(x => x).ToList();
                    
                    // Initialize default settings for newly discovered types
                    foreach (var typeName in HealthEnhanceSettings.DiscoveredEventTypes)
                    {
                        if (!Settings.EnabledEventTypes.ContainsKey(typeName))
                        {
                            // Enable by default, except Verse.Message
                            bool defaultEnabled = !typeName.Equals("Verse.Message", System.StringComparison.OrdinalIgnoreCase);
                            Settings.EnabledEventTypes[typeName] = defaultEnabled;
                        }
                    }
                }
                
                _archivableTypesScanned = true;
                Log.Message($"[RimTalk Health Enhance] Discovered {HealthEnhanceSettings.DiscoveredEventTypes.Count} archivable types.");
            }
            catch (System.Exception ex)
            {
                Log.Warning($"[RimTalk Health Enhance] Error scanning archivable types: {ex.Message}");
            }
        }
    }
}
