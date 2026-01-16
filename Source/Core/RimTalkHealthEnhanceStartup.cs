using System;
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
            
            // 先尝试注册 RimTalk API hooks
            bool apiRegistered = false;
            try
            {
                RimTalkEnhanceAPI.Initialize();
                apiRegistered = RimTalkEnhanceAPI.IsRegistered;
                
                if (apiRegistered)
                {
                    Log.Message("[RimTalk Enhanced Prompt] ✓ RimTalk API hooks registered successfully");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhanced Prompt] Failed to register API hooks, falling back to Harmony patches: {ex.Message}");
            }
            
            // 应用 Harmony patches
            // 如果 API 注册成功，跳过已迁移的 patches；否则应用所有 patches
            if (apiRegistered)
            {
                // 只应用事件监听类 patches（无法用 API 替代的）
                ApplyEventPatches(harmony);
            }
            else
            {
                // 回退：应用所有 patches
                harmony.PatchAll();
            }
            
            // 验证关键 Patch 是否成功应用
            var patchedMethods = harmony.GetPatchedMethods().ToList();
            Log.Message($"[RimTalk Enhanced Prompt] Harmony patches applied. Total patched methods: {patchedMethods.Count}");
            
            // 验证事件监听 patches
            VerifyEventPatches(harmony);
        }
        
        /// <summary>
        /// 只应用事件监听类 patches（无法用 API 替代的）
        /// </summary>
        private static void ApplyEventPatches(Harmony harmony)
        {
            // 这些 patches 用于事件监听，无法用 RimTalk API 替代
            // 使用反射手动应用特定的 patches
            
            var patchTypes = new[]
            {
                // 事件监听类 - 无法用 API 替代
                typeof(ArchiveCapturePatch),
                typeof(BlueprintPlacePatch),  // BlueprintActionPatch 文件中的类
                typeof(BlueprintCancelPatch),
                typeof(PawnKillPatch),
                typeof(PawnExitMapPatch),
                typeof(PawnDamagePatch),
                typeof(LayoutCachePatch),
                typeof(Patches.PlaySettingsColonyCenterPatch),
                typeof(WeatherTransitionPatch),
                typeof(GameConditionRegisterPatch),  // GameConditionCapturePatch 文件中的类
                typeof(GameConditionEndPatch),
                // 以下 Patches 已迁移到 API:
                // - HealthContextPatch -> ContextCategories.Pawn.Health (Override)
                // - EquipmentContextPatch -> ContextCategories.Pawn.Equipment (Override)
                // - RelationsContextPatch -> ContextCategories.Pawn.Relations (Override)
                // - TraitsContextPatch -> ContextCategories.Pawn.Traits (Override)
                // - LocationContextPatch -> ContextCategories.Pawn.Location (Append)
                // - ActivityStatusPatch -> 移除（RimTalk 内置已有移动状态检测）
                // - AnnouncementContextPatch -> RegisterContextVariable (colony_status)
                // - PromptSnapshotPatch -> RegisterContextVariable (colony_history) + PromptEntry
            };
            
            foreach (var patchType in patchTypes)
            {
                try
                {
                    harmony.CreateClassProcessor(patchType).Patch();
                }
                catch (Exception ex)
                {
                    Log.Warning($"[RimTalk Enhanced Prompt] Failed to patch {patchType.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 验证事件监听 patches
        /// </summary>
        private static void VerifyEventPatches(Harmony harmony)
        {
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
            
            // 检查 MakeDowned 是否被 patch
            var makeDownedMethod = typeof(Pawn_HealthTracker).GetMethod("MakeDowned");
            if (makeDownedMethod != null)
            {
                var patchInfo = Harmony.GetPatchInfo(makeDownedMethod);
                if (patchInfo != null && patchInfo.Postfixes.Any(p => p.owner == "ruaji.rimtalkpromptenhance"))
                {
                    Log.Message("[RimTalk Enhanced Prompt] ✓ Pawn_HealthTracker.MakeDowned patch verified.");
                }
            }
        }
    }
}
