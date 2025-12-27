using System;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace RimTalkHealthEnhance.Patches
{
    /// <summary>
    /// Patch RimTalk's PawnUtil.GetActivity to add "(Traveling)" suffix
    /// when pawn is moving to work target instead of actively working.
    /// </summary>
    [HarmonyPatch]
    public static class ActivityStatusPatch
    {
        private static Type pawnUtilType;
        private static bool initialized = false;
        private static bool patchFailed = false;
        
        // Reflection for accessing protected JobDriver.CurToil property
        private static PropertyInfo curToilProperty;

        /// <summary>
        /// Dynamically find RimTalk.Util.PawnUtil type and cache reflection info
        /// </summary>
        static ActivityStatusPatch()
        {
            try
            {
                pawnUtilType = AccessTools.TypeByName("RimTalk.Util.PawnUtil");
                if (pawnUtilType == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not find RimTalk.Util.PawnUtil type. Traveling status feature disabled.");
                    patchFailed = true;
                    return;
                }
                
                // Cache the CurToil property via reflection (it's protected)
                curToilProperty = typeof(JobDriver).GetProperty("CurToil", BindingFlags.NonPublic | BindingFlags.Instance);
                if (curToilProperty == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not find JobDriver.CurToil property. Traveling status feature disabled.");
                    patchFailed = true;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhance] Failed to initialize ActivityStatusPatch: {ex.Message}");
                patchFailed = true;
            }
        }

        /// <summary>
        /// Target method for Harmony patch
        /// </summary>
        [HarmonyTargetMethod]
        public static System.Reflection.MethodBase TargetMethod()
        {
            if (patchFailed || pawnUtilType == null)
                return null;

            try
            {
                var method = AccessTools.Method(pawnUtilType, "GetActivity", new Type[] { typeof(Pawn) });
                if (method == null)
                {
                    Log.Warning("[RimTalk Enhance] Could not find GetActivity method in PawnUtil. Traveling status feature disabled.");
                    patchFailed = true;
                    return null;
                }
                initialized = true;
                return method;
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhance] Failed to find GetActivity method: {ex.Message}");
                patchFailed = true;
                return null;
            }
        }

        /// <summary>
        /// Postfix: Add "TRAVELING to [activity]" prefix when pawn is moving to work target
        /// This makes it clearer to AI that the pawn hasn't started the activity yet
        /// </summary>
        [HarmonyPostfix]
        public static void Postfix(Pawn pawn, ref string __result)
        {
            if (!initialized || patchFailed) return;
            if (string.IsNullOrEmpty(__result)) return;
            if (pawn == null) return;

            try
            {
                if (IsGoingToWork(pawn))
                {
                    // Check if we should skip this prefix for simple movement jobs
                    if (ShouldSkipPrefix(pawn))
                        return;

                    // Use pattern format to make it clear the pawn is moving towards a goal
                    // Format: "[移动中] 目标: 原活动描述" or "[Moving] Target: original activity"
                    __result = "RimTalkEnhance.TravelingPattern".Translate(__result);
                }
            }
            catch (Exception ex)
            {
                // Silently fail to avoid breaking RimTalk
                if (Prefs.DevMode)
                {
                    Log.Warning($"[RimTalk Enhance] ActivityStatusPatch error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Check if the current job is a simple movement job that shouldn't have the prefix
        /// </summary>
        private static bool ShouldSkipPrefix(Pawn pawn)
        {
            if (pawn.CurJob == null || pawn.CurJob.def == null)
                return true;

            string defName = pawn.CurJob.def.defName;
            
            // Skip basic movement jobs where "Target: Walking" would sound weird
            return defName == "Goto" ||
                   defName == "GotoSafeTemperature" ||
                   defName == "GotoWander" ||
                   defName == "Wait_Wander" ||
                   defName == "Flee" ||
                   defName == "FleeAndCower";
        }

        /// <summary>
        /// Check if pawn is currently moving to work target (not actively working)
        /// </summary>
        /// <param name="pawn">The pawn to check</param>
        /// <returns>True if pawn is traveling to work, false if already working or not moving</returns>
        private static bool IsGoingToWork(Pawn pawn)
        {
            // Check if pawn has a pather and is moving
            if (pawn.pather == null || !pawn.pather.Moving)
                return false;

            // Check if pawn has an active job driver
            var curDriver = pawn.jobs?.curDriver;
            if (curDriver == null)
                return false;

            // Check if we have the reflection property cached
            if (curToilProperty == null)
                return false;

            // Get current toil via reflection (it's protected)
            var curToil = curToilProperty.GetValue(curDriver) as Toil;
            if (curToil == null)
                return false;

            // PatherArrival means the toil is waiting for the pawn to arrive at destination
            // This indicates the pawn is traveling, not working
            return curToil.defaultCompleteMode == ToilCompleteMode.PatherArrival;
        }
    }
}