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

        public void ExposeData()
        {
            Scribe_Values.Look(ref UnlimitedRelations, "unlimitedRelations", false);
            Scribe_Values.Look(ref UnlimitedTraits, "unlimitedTraits", false);
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

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(60f), 
                "说明：\n" +
                "1. 解除关系数量限制：显示所有关系，不再受 RimTalk 设置中的 MaxPawnContextCount 限制。\n" +
                "2. 解除配角特质限制：在 Short 模式下显示所有特质，不再只显示前 3 个。\n" +
                "注意：开启这些选项可能会增加 Token 消耗。");
            Text.Font = GameFont.Small;
        }
    }
}