using System.Collections.Generic;
using HarmonyLib;
using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Harmony patch to inject colony announcement context into AI prompts.
    /// Targets PromptService.BuildContext which builds the context for all pawns.
    /// </summary>
    [HarmonyPatch(typeof(PromptService), "BuildContext")]
    public static class AnnouncementContextPatch
    {
        /// <summary>
        /// Postfix patch that appends colony status information to the built context.
        /// </summary>
        /// <param name="pawns">List of pawns involved in the conversation</param>
        /// <param name="__result">The original context string built by BuildContext</param>
        static void Postfix(List<Pawn> pawns, ref string __result)
        {
            // 防止重复注入
            if (__result != null && __result.Contains("=== Colony Status ==="))
                return;

            string announcement = AnnouncementBuilder.BuildAnnouncementContext();
            if (!string.IsNullOrEmpty(announcement))
            {
                __result += "\n\n" + announcement;
            }
        }
    }
}
