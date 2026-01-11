using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 公告/状况板相关设置
    /// </summary>
    public class AnnouncementSettings : IExposable
    {
        public bool ShowColonyAnnouncements = true;
        public bool ShowColonyOverview = true;        // 显示自由文本概况
        public bool ShowStructuredTasks = true;       // 显示结构化任务
        public bool OnlyShowActiveTasks = true;       // 只显示进行中的任务
        public float CompletedTaskShowDays = 1f;      // 已完成任务保留显示天数
        public int MaxOverviewLength = 500;           // 概况文本最大长度

        public void ExposeData()
        {
            Scribe_Values.Look(ref ShowColonyAnnouncements, "showColonyAnnouncements", true);
            Scribe_Values.Look(ref ShowColonyOverview, "showColonyOverview", true);
            Scribe_Values.Look(ref ShowStructuredTasks, "showStructuredTasks", true);
            Scribe_Values.Look(ref OnlyShowActiveTasks, "onlyShowActiveTasks", true);
            Scribe_Values.Look(ref CompletedTaskShowDays, "completedTaskShowDays", 1f);
            Scribe_Values.Look(ref MaxOverviewLength, "maxOverviewLength", 500);
        }

        /// <summary>
        /// 绘制公告设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Announcement_Enable".Translate(), ref ShowColonyAnnouncements, "RTE_Settings_Announcement_Enable_Desc".Translate());
            
            if (ShowColonyAnnouncements)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Announcement_ShowOverview".Translate(), ref ShowColonyOverview, "RTE_Settings_Announcement_ShowOverview_Desc".Translate());
                if (ShowColonyOverview)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Announcement_OverviewMaxLength".Translate(MaxOverviewLength));
                    MaxOverviewLength = (int)listing.Slider(MaxOverviewLength, 100, 2000);
                }
                
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Announcement_ShowTasks".Translate(), ref ShowStructuredTasks, "RTE_Settings_Announcement_ShowTasks_Desc".Translate());
                if (ShowStructuredTasks)
                {
                    listing.CheckboxLabeled("RTE_Settings_Announcement_OnlyActive".Translate(), ref OnlyShowActiveTasks, "RTE_Settings_Announcement_OnlyActive_Desc".Translate());
                    if (OnlyShowActiveTasks)
                    {
                        Widgets.Label(listing.GetRect(22f), "RTE_Settings_Announcement_CompletedDays".Translate(CompletedTaskShowDays.ToString("F1")));
                        CompletedTaskShowDays = listing.Slider(CompletedTaskShowDays, 0f, 7f);
                    }
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), "RTE_Settings_Announcement_Tip".Translate());
            Text.Font = GameFont.Small;
        }
    }
}