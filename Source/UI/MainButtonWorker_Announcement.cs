using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    public class MainButtonWorker_Announcement : MainButtonWorker_ToggleTab
    {
        public override bool Visible
        {
            get
            {
                // 只有在设置中启用了通告系统时才显示
                return RimTalkHealthEnhanceMod.Settings.ShowColonyAnnouncements;
            }
        }
    }
}
