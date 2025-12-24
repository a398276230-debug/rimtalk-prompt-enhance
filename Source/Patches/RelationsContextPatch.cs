using System;
using System.Linq;
using System.Text;
using HarmonyLib;
using RimTalk;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// Harmony patch to replace RimTalk's GetRelationsContext method with unlimited version
    /// </summary>
    [HarmonyPatch(typeof(ContextBuilder), "GetRelationsContext")]
    public static class RelationsContextPatch
    {
        private const float FriendOpinionThreshold = 20f;
        private const float RivalOpinionThreshold = -20f;

        /// <summary>
        /// Prefix patch that replaces the original method entirely if UnlimitedRelations is enabled
        /// </summary>
        static bool Prefix(Pawn pawn, PromptService.InfoLevel infoLevel, ref string __result)
        {
            // Check if context is enabled in RimTalk settings
            var contextSettings = Settings.Get().Context;
            if (!contextSettings.IncludeRelations)
            {
                __result = null;
                return false; // Skip original method
            }

            // Check if unlimited relations is enabled in our settings
            if (!RimTalkHealthEnhanceMod.Settings.UnlimitedRelations)
            {
                return true; // Execute original method
            }

            // Use our unlimited relations builder
            __result = GetRelationsStringUnlimited(pawn);
            
            return false; // Skip original method
        }

        /// <summary>
        /// A copy of RelationsService.GetRelationsString but without the .Take() limit
        /// </summary>
        private static string GetRelationsStringUnlimited(Pawn pawn)
        {
            if (pawn?.relations == null) return "";

            StringBuilder relationsSb = new StringBuilder();

            // Use PawnSelector.GetAllNearByPawns(pawn) but WITHOUT .Take()
            foreach (Pawn otherPawn in PawnSelector.GetAllNearByPawns(pawn))
            {
                if (otherPawn == pawn || (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) || otherPawn.Dead ||
                    otherPawn.relations is { hidePawnRelations: true }) continue;

                string label = null;

                try
                {
                    float opinionValue = pawn.relations.OpinionOf(otherPawn);

                    // --- Step 1: Check for the most important direct or family relationship ---
                    PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(otherPawn);
                    if (mostImportantRelation != null)
                    {
                        label = mostImportantRelation.GetGenderSpecificLabelCap(otherPawn);
                    }

                    // --- Step 2: If no family relation, check for an overriding status (master, slave, etc.) ---
                    if (string.IsNullOrEmpty(label))
                    {
                        label = GetStatusLabel(pawn, otherPawn);
                    }

                    // --- Step 3: If no other label found, fall back to opinion-based relationship ---
                    if (string.IsNullOrEmpty(label) && !pawn.IsVisitor() && !pawn.IsEnemy())
                    {
                        if (opinionValue >= FriendOpinionThreshold)
                        {
                            label = "Friend".Translate();
                        }
                        else if (opinionValue <= RivalOpinionThreshold)
                        {
                            label = "Rival".Translate();
                        }
                        else
                        {
                            label = "Acquaintance".Translate();
                        }
                    }

                    // If we found any relevant relationship, add it to the string.
                    if (!string.IsNullOrEmpty(label))
                    {
                        string pawnName = otherPawn.LabelShort;
                        string opinion = opinionValue.ToStringWithSign();
                        relationsSb.Append($"{pawnName}({label}) {opinion}, ");
                    }
                }
                catch (Exception)
                {
                    // Skip this pawn if opinion calculation fails due to mod conflicts
                }
            }

            if (relationsSb.Length > 0)
            {
                // Remove the trailing comma and space
                relationsSb.Length -= 2;
                return "Relations: " + relationsSb;
            }

            return "";
        }

        private static string GetStatusLabel(Pawn pawn, Pawn otherPawn)
        {
            // Master relationship
            if ((pawn.IsPrisoner || pawn.IsSlave) && otherPawn.IsFreeNonSlaveColonist)
            {
                return "Master".Translate();
            }

            // Prisoner or slave labels
            if (otherPawn.IsPrisoner) return "Prisoner".Translate();
            if (otherPawn.IsSlave) return "Slave".Translate();

            // Hostile relationship
            if (pawn.Faction != null && otherPawn.Faction != null && pawn.Faction.HostileTo(otherPawn.Faction))
            {
                return "Enemy".Translate();
            }

            // No special status found
            return null;
        }
    }
}
