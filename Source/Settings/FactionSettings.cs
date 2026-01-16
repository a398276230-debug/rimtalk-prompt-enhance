using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 派系关系相关设置
    /// </summary>
    public class FactionSettings : IExposable
    {
        public bool ShowFactionRelations = true;
        public bool ShowFactionGoodwill = true;
        public bool ShowFactionMemberCount = true;
        public bool ShowIdentityBreakdown = true;     // 显示身份细分（囚犯/敌人/访客）
        public bool ShowGlobalSummary = false;        // 显示全局身份摘要
        public bool ShowNeutralFactions = true;
        public bool FilterByGoodwill = false;
        public int MinGoodwillToShow = -100;
        public float FactionCacheUpdateInterval = 5f;  // 派系信息缓存更新间隔（秒）

        public void ExposeData()
        {
            Scribe_Values.Look(ref ShowFactionRelations, "showFactionRelations", true);
            Scribe_Values.Look(ref ShowFactionGoodwill, "showFactionGoodwill", true);
            Scribe_Values.Look(ref ShowFactionMemberCount, "showFactionMemberCount", true);
            Scribe_Values.Look(ref ShowIdentityBreakdown, "showIdentityBreakdown", true);
            Scribe_Values.Look(ref ShowGlobalSummary, "showGlobalSummary", false);
            Scribe_Values.Look(ref ShowNeutralFactions, "showNeutralFactions", true);
            Scribe_Values.Look(ref FilterByGoodwill, "filterByGoodwill", false);
            Scribe_Values.Look(ref MinGoodwillToShow, "minGoodwillToShow", -100);
            Scribe_Values.Look(ref FactionCacheUpdateInterval, "factionCacheUpdateInterval", 5f);
        }

        /// <summary>
        /// 绘制派系设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Factions_Enable".Translate(), ref ShowFactionRelations, 
                "RTE_Settings_Factions_Enable_Desc".Translate());
            
            if (ShowFactionRelations)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowGoodwill".Translate(), ref ShowFactionGoodwill, 
                    "RTE_Settings_Factions_ShowGoodwill_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowMemberCount".Translate(), ref ShowFactionMemberCount, 
                    "RTE_Settings_Factions_ShowMemberCount_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowIdentity".Translate(), ref ShowIdentityBreakdown,
                    "RTE_Settings_Factions_ShowIdentity_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowSummary".Translate(), ref ShowGlobalSummary,
                    "RTE_Settings_Factions_ShowSummary_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Factions_ShowNeutral".Translate(), ref ShowNeutralFactions, 
                    "RTE_Settings_Factions_ShowNeutral_Desc".Translate());
                
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Factions_CacheInterval".Translate(FactionCacheUpdateInterval.ToString("F1")));
                FactionCacheUpdateInterval = listing.Slider(FactionCacheUpdateInterval, 1f, 30f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(listing.GetRect(18f), "RTE_Settings_Factions_CacheInterval_Desc".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Factions_FilterByGoodwill".Translate(), ref FilterByGoodwill,
                    "RTE_Settings_Factions_FilterByGoodwill_Desc".Translate());
                
                if (FilterByGoodwill)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Factions_MinGoodwill".Translate(MinGoodwillToShow));
                    MinGoodwillToShow = (int)listing.Slider(MinGoodwillToShow, -100, 100);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_Factions_MinGoodwill_Desc".Translate(MinGoodwillToShow));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(80f), "RTE_Settings_Factions_Note".Translate());
            Text.Font = GameFont.Small;
        }
    }
}