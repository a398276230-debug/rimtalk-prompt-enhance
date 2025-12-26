using System.Linq;
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
            
            // 验证关键 Patch 是否成功应用
            var patchedMethods = harmony.GetPatchedMethods().ToList();
            Log.Message($"[RimTalk Enhanced Prompt] Harmony patches applied successfully. Total patched methods: {patchedMethods.Count}");
            
            // 检查 Pawn.Kill 是否被 patch
            var pawnKillMethod = typeof(Pawn).GetMethod("Kill");
            if (pawnKillMethod != null)
            {
                var patchInfo = Harmony.GetPatchInfo(pawnKillMethod);
                if (patchInfo != null && patchInfo.Postfixes.Any(p => p.owner == "ruaji.rimtalkpromptenhance"))
                {
                    Log.Message("[RimTalk Enhanced Prompt] ✓ Pawn.Kill patch verified.");
                }
                else
                {
                    Log.Warning("[RimTalk Enhanced Prompt] ✗ Pawn.Kill patch NOT found! Combat tracking may not work.");
                }
            }
            else
            {
                Log.Warning("[RimTalk Enhanced Prompt] ✗ Pawn.Kill method not found!");
            }
            
            // 检查 MakeDowned 是否被 patch
            var makeDownedMethod = typeof(Pawn_HealthTracker).GetMethod("MakeDowned");
            if (makeDownedMethod != null)
            {
                var patchInfo = Harmony.GetPatchInfo(makeDownedMethod);
                if (patchInfo != null && patchInfo.Postfixes.Any(p => p.owner == "ruaji.rimtalkpromptenhance"))
                {
                    Log.Message("[RimTalk Enhanced Prompt] ✓ Pawn_HealthTracker.MakeDowned patch verified.");
                }
                else
                {
                    Log.Warning("[RimTalk Enhanced Prompt] ✗ MakeDowned patch NOT found!");
                }
            }
        }
    }
}
