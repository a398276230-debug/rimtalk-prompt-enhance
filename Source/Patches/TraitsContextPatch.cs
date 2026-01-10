using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimTalk;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Harmony patch to replace RimTalk's GetTraitsContext method with unlimited version
    /// </summary>
    [HarmonyPatch(typeof(ContextBuilder), "GetTraitsContext")]
    public static class TraitsContextPatch
    {
        /// <summary>
        /// Prefix patch that replaces the original method entirely if UnlimitedTraits is enabled
        /// </summary>
        static bool Prefix(Pawn pawn, PromptService.InfoLevel infoLevel, ref string __result)
        {
            // Check if context is enabled in RimTalk settings
            var contextSettings = Settings.Get().Context;
            if (!contextSettings.IncludeTraits)
            {
                __result = null;
                return false; // Skip original method
            }

            // Check if unlimited traits is enabled in our settings
            if (!RimTalkHealthEnhanceMod.Settings.UnlimitedTraits)
            {
                return true; // Execute original method
            }

            // Use our unlimited traits builder
            __result = GetTraitsContextUnlimited(pawn, infoLevel);
            
            return false; // Skip original method
        }

        /// <summary>
        /// A copy of ContextBuilder.GetTraitsContext but without the .Take(3) limit
        /// </summary>
        private static string GetTraitsContextUnlimited(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var traits = new List<string>();
            foreach (var trait in pawn.story?.traits?.TraitsSorted ?? Enumerable.Empty<Trait>())
            {
                var degreeData = GenCollection.FirstOrDefault(trait.def.degreeDatas, d => d.degree == trait.Degree);
                if (degreeData != null)
                {
                    var traitText = infoLevel == PromptService.InfoLevel.Full
                        ? $"{degreeData.label}:{CommonUtil.Sanitize(degreeData.description, pawn)}"
                        : degreeData.label;
                    traits.Add(traitText);
                }
            }

            // Original code had this limit:
            // if (infoLevel == PromptService.InfoLevel.Short && traits.Count > 3)
            //     traits = traits.Take(3).ToList();
            // We skip this limit here.

            if (traits.Any())
            {
                var separator = infoLevel == PromptService.InfoLevel.Full ? "\n" : ",";
                return $"Traits: {string.Join(separator, traits)}";
            }
            return null;
        }
    }
}
