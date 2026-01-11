using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 健康信息显示相关设置
    /// </summary>
    public class HealthSettings : IExposable
    {
        // === 基础健康设置 ===
        public bool ShowSeverity = true;
        public bool ShowPainLevel = true;
        public bool ShowLethalMarker = true;
        public bool ShowDescription = true;
        public float MinPainToShow = 0.01f;
        public float LethalThreshold = 0.8f;
        public int MaxDescriptionLength = 100;

        // === 分类过滤设置 ===
        public bool ShowBionics = true;           // 显示仿生体/义肢
        public bool ShowImplants = true;          // 显示其他植入物
        public bool ShowInjuries = true;          // 显示伤口
        public bool ShowMissingParts = true;      // 显示缺失部位
        public bool ShowConditions = true;        // 显示疾病/状态

        // 数量限制 (0 = 无限制)
        public int MaxBionicsToShow = 10;         // 最多显示仿生体数量
        public int MaxImplantsToShow = 10;        // 最多显示植入物数量
        public int MaxInjuriesToShow = 20;        // 最多显示伤口数量
        public int MaxConditionsToShow = 10;      // 最多显示状态数量

        // === 智能整合选项 ===
        public bool EnableInjuryConsolidation = true;  // 启用伤口整合
        public bool EnableBionicSummary = true;        // 启用仿生体摘要模式
        public float MinorInjurySeverityThreshold = 0.3f; // 轻伤阈值

        public void ExposeData()
        {
            Scribe_Values.Look(ref ShowSeverity, "showSeverity", true);
            Scribe_Values.Look(ref ShowPainLevel, "showPainLevel", true);
            Scribe_Values.Look(ref ShowLethalMarker, "showLethalMarker", true);
            Scribe_Values.Look(ref ShowDescription, "showDescription", true);
            Scribe_Values.Look(ref MinPainToShow, "minPainToShow", 0.01f);
            Scribe_Values.Look(ref LethalThreshold, "lethalThreshold", 0.8f);
            Scribe_Values.Look(ref MaxDescriptionLength, "maxDescriptionLength", 100);

            Scribe_Values.Look(ref ShowBionics, "showBionics", true);
            Scribe_Values.Look(ref ShowImplants, "showImplants", true);
            Scribe_Values.Look(ref ShowInjuries, "showInjuries", true);
            Scribe_Values.Look(ref ShowMissingParts, "showMissingParts", true);
            Scribe_Values.Look(ref ShowConditions, "showConditions", true);
            Scribe_Values.Look(ref MaxBionicsToShow, "maxBionicsToShow", 10);
            Scribe_Values.Look(ref MaxImplantsToShow, "maxImplantsToShow", 10);
            Scribe_Values.Look(ref MaxInjuriesToShow, "maxInjuriesToShow", 20);
            Scribe_Values.Look(ref MaxConditionsToShow, "maxConditionsToShow", 10);
            Scribe_Values.Look(ref EnableInjuryConsolidation, "enableInjuryConsolidation", true);
            Scribe_Values.Look(ref EnableBionicSummary, "enableBionicSummary", true);
            Scribe_Values.Look(ref MinorInjurySeverityThreshold, "minorInjurySeverityThreshold", 0.3f);
        }

        /// <summary>
        /// 绘制健康设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Health_ShowSeverity".Translate(), ref ShowSeverity,
                "RTE_Settings_Health_ShowSeverity_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowPainLevel".Translate(), ref ShowPainLevel,
                "RTE_Settings_Health_ShowPainLevel_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowLethalMarker".Translate(), ref ShowLethalMarker,
                "RTE_Settings_Health_ShowLethalMarker_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Health_ShowDescription".Translate(), ref ShowDescription,
                "RTE_Settings_Health_ShowDescription_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MinPainThreshold".Translate(MinPainToShow.ToString("F2")));
            MinPainToShow = listing.Slider(MinPainToShow, 0f, 0.5f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_LethalThreshold".Translate(LethalThreshold.ToStringPercent()));
            LethalThreshold = listing.Slider(LethalThreshold, 0.5f, 1f);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxDescLength".Translate(MaxDescriptionLength));
            MaxDescriptionLength = (int)listing.Slider(MaxDescriptionLength, 50, 200);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // ========== 分类过滤 ==========
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(listing.GetRect(26f), "RTE_Settings_Health_FilterSection".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            // 仿生体/义肢
            listing.CheckboxLabeled("RTE_Settings_Health_ShowBionics".Translate(), ref ShowBionics,
                "RTE_Settings_Health_ShowBionics_Desc".Translate());
            if (ShowBionics)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxBionicsToShow == 0 ? "∞" : MaxBionicsToShow.ToString()));
                MaxBionicsToShow = (int)listing.Slider(MaxBionicsToShow, 0, 20);
            }

            // 其他植入物
            listing.CheckboxLabeled("RTE_Settings_Health_ShowImplants".Translate(), ref ShowImplants,
                "RTE_Settings_Health_ShowImplants_Desc".Translate());
            if (ShowImplants)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxImplantsToShow == 0 ? "∞" : MaxImplantsToShow.ToString()));
                MaxImplantsToShow = (int)listing.Slider(MaxImplantsToShow, 0, 20);
            }

            // 伤口
            listing.CheckboxLabeled("RTE_Settings_Health_ShowInjuries".Translate(), ref ShowInjuries,
                "RTE_Settings_Health_ShowInjuries_Desc".Translate());
            if (ShowInjuries)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxInjuriesToShow == 0 ? "∞" : MaxInjuriesToShow.ToString()));
                MaxInjuriesToShow = (int)listing.Slider(MaxInjuriesToShow, 0, 30);
            }

            // 缺失部位
            listing.CheckboxLabeled("RTE_Settings_Health_ShowMissingParts".Translate(), ref ShowMissingParts,
                "RTE_Settings_Health_ShowMissingParts_Desc".Translate());

            // 疾病/状态
            listing.CheckboxLabeled("RTE_Settings_Health_ShowConditions".Translate(), ref ShowConditions,
                "RTE_Settings_Health_ShowConditions_Desc".Translate());
            if (ShowConditions)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MaxToShow".Translate(MaxConditionsToShow == 0 ? "∞" : MaxConditionsToShow.ToString()));
                MaxConditionsToShow = (int)listing.Slider(MaxConditionsToShow, 0, 20);
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            // ========== 智能整合 ==========
            Text.Font = GameFont.Medium;
            GUI.color = new Color(0.8f, 0.9f, 1f);
            Widgets.Label(listing.GetRect(26f), "RTE_Settings_Health_ConsolidationSection".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            listing.Gap(4f);

            listing.CheckboxLabeled("RTE_Settings_Health_EnableInjuryConsolidation".Translate(), ref EnableInjuryConsolidation,
                "RTE_Settings_Health_EnableInjuryConsolidation_Desc".Translate());
            
            if (EnableInjuryConsolidation)
            {
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Health_MinorInjuryThreshold".Translate(MinorInjurySeverityThreshold.ToStringPercent()));
                MinorInjurySeverityThreshold = listing.Slider(MinorInjurySeverityThreshold, 0.1f, 0.5f);
                
                Text.Font = GameFont.Tiny;
                GUI.color = Color.gray;
                Widgets.Label(listing.GetRect(18f), "RTE_Settings_Health_MinorInjuryThreshold_Desc".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            listing.CheckboxLabeled("RTE_Settings_Health_EnableBionicSummary".Translate(), ref EnableBionicSummary,
                "RTE_Settings_Health_EnableBionicSummary_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note1".Translate());
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note2".Translate());
            Widgets.Label(listing.GetRect(20f), "RTE_Settings_Health_Note3".Translate());
            Text.Font = GameFont.Small;
        }
    }
}