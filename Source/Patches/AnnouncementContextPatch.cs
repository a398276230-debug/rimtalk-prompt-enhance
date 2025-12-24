using HarmonyLib;
using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    [HarmonyPatch(typeof(AIService), "UpdateContext")]
    public static class AnnouncementContextPatch
    {
        static void Prefix(ref string context)
        {
            // 防止重复注入
            if (context != null && context.Contains("=== Colony Status ==="))
                return;

            string announcement = AnnouncementBuilder.BuildAnnouncementContext();
            if (!string.IsNullOrEmpty(announcement))
            {
                context += "\n\n" + announcement;
            }
        }
    }
}
