using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 杂项设置
    /// </summary>
    public class MiscSettings : IExposable
    {
        public bool UnlimitedRelations = false;          // 解除关系数量限制
        public bool UnlimitedTraits = false;             // 解除配角特质限制
        public bool DebugMode = false;                   // 调试模式（输出详细 debug 日志）

        public void ExposeData()
        {
            Scribe_Values.Look(ref UnlimitedRelations, "unlimitedRelations", false);
            Scribe_Values.Look(ref UnlimitedTraits, "unlimitedTraits", false);
            Scribe_Values.Look(ref DebugMode, "debugMode", false);
        }

        /// <summary>
        /// 绘制杂项设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Misc_UnlimitedRelations".Translate(), ref UnlimitedRelations,
                "RTE_Settings_Misc_UnlimitedRelations_Desc".Translate());

            listing.CheckboxLabeled("RTE_Settings_Misc_UnlimitedTraits".Translate(), ref UnlimitedTraits,
                "RTE_Settings_Misc_UnlimitedTraits_Desc".Translate());

            listing.CheckboxLabeled("RTE_Settings_Misc_DebugMode".Translate(), ref DebugMode,
                "RTE_Settings_Misc_DebugMode_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(60f), "RTE_Settings_Misc_Note".Translate());
            Text.Font = GameFont.Small;
        }
    }
}