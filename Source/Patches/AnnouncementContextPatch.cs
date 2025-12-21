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
            string announcement = AnnouncementBuilder.BuildAnnouncementContext();
            if (!string.IsNullOrEmpty(announcement))
            {
                context += "\n\n" + announcement;
            }
        }
    }
}
