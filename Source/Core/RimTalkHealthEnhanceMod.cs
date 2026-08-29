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

        public RimTalkHealthEnhanceMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<HealthEnhanceSettings>();
        }

        public override string SettingsCategory() => "RimTalk 增强提示词";

        public override void DoSettingsWindowContents(Rect inRect)
        {
            // Scan for types if opening the events tab or first time（主动扫描：程序集 + Defs + 存档实例）
            ArchivableTypeScanner.ScanIfNeeded();

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
    }
}
