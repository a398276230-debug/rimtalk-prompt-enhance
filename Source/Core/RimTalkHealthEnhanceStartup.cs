using HarmonyLib;
using Verse;

namespace RimTalkHealthEnhance
{
    [StaticConstructorOnStartup]
    public static class RimTalkHealthEnhanceStartup
    {
        static RimTalkHealthEnhanceStartup()
        {
            var harmony = new Harmony("ruaji.rimtalkpromptenhance");
            harmony.PatchAll();
            
            Log.Message("[RimTalk Enhanced Prompt] Harmony patches applied successfully.");
        }
    }
}
