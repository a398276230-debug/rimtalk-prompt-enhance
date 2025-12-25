using System;
using System.Collections.Generic;
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
        /// Enhanced version that reads all DirectRelations plus nearby pawns
        /// </summary>
        private static string GetRelationsStringUnlimited(Pawn pawn)
        {
            if (pawn?.relations == null) return "";

            StringBuilder relationsSb = new StringBuilder();
            HashSet<Pawn> processedPawns = new HashSet<Pawn>();

            // Step 1: Process all DirectRelations (family, friends, rivals, etc.)
            if (pawn.relations.DirectRelations != null)
            {
                foreach (var relation in pawn.relations.DirectRelations)
                {
                    Pawn otherPawn = relation.otherPawn;
                    if (ShouldProcessPawn(pawn, otherPawn))
                    {
                        processedPawns.Add(otherPawn);
                        AppendRelationInfo(pawn, otherPawn, relationsSb);
                    }
                }
            }

            // Step 2: Process all colony pawns for opinion-based relationships
            var allColonyPawns = Find.CurrentMap?.mapPawns?.AllPawnsSpawned;
            if (allColonyPawns != null)
            {
                foreach (Pawn otherPawn in allColonyPawns)
                {
                    if (!processedPawns.Contains(otherPawn) && ShouldProcessPawn(pawn, otherPawn))
                    {
                        processedPawns.Add(otherPawn);
                        AppendRelationInfo(pawn, otherPawn, relationsSb);
                    }
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

        /// <summary>
        /// Check if a pawn should be processed for relations
        /// </summary>
        private static bool ShouldProcessPawn(Pawn pawn, Pawn otherPawn)
        {
            if (otherPawn == null || otherPawn == pawn) return false;
            if (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) return false;
            if (otherPawn.Dead) return false;
            if (otherPawn.relations is { hidePawnRelations: true }) return false;
            return true;
        }

        /// <summary>
        /// Append relation information for a pawn
        /// </summary>
        private static void AppendRelationInfo(Pawn pawn, Pawn otherPawn, StringBuilder sb)
        {
            try
            {
                float opinionValue = pawn.relations.OpinionOf(otherPawn);
                string label = null;

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
                    sb.Append($"{pawnName}({label}) {opinion}, ");
                }
            }
            catch (Exception)
            {
                // Skip this pawn if opinion calculation fails due to mod conflicts
            }
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
