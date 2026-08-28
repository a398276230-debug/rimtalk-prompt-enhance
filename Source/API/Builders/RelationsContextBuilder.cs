using System;
using System.Collections.Generic;
using System.Text;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.API
{
    /// <summary>
    /// 构建关系上下文信息（无限制版本）
    /// </summary>
    internal static class RelationsContextBuilder
    {
        private const float FriendOpinionThreshold = 20f;
        private const float RivalOpinionThreshold = -20f;

        /// <summary>
        /// 获取无限制的关系字符串
        /// </summary>
        public static string GetRelationsStringUnlimited(Pawn pawn)
        {
            if (pawn?.relations == null) return "";

            StringBuilder relationsSb = new StringBuilder();
            HashSet<Pawn> processedPawns = new HashSet<Pawn>();

            // Step 1: Process all DirectRelations
            ProcessDirectRelations(pawn, relationsSb, processedPawns);

            // Step 2: Process all colony pawns for opinion-based relationships
            ProcessColonyPawns(pawn, relationsSb, processedPawns);

            if (relationsSb.Length > 0)
            {
                relationsSb.Length -= 2; // Remove trailing ", "
                return "Relations: " + relationsSb;
            }

            return "";
        }

        private static void ProcessDirectRelations(Pawn pawn, StringBuilder sb, HashSet<Pawn> processedPawns)
        {
            if (pawn.relations.DirectRelations == null) return;

            foreach (var relation in pawn.relations.DirectRelations)
            {
                Pawn otherPawn = relation.otherPawn;
                if (ShouldProcessPawn(pawn, otherPawn))
                {
                    processedPawns.Add(otherPawn);
                    AppendRelationInfo(pawn, otherPawn, sb);
                }
            }
        }

        private static void ProcessColonyPawns(Pawn pawn, StringBuilder sb, HashSet<Pawn> processedPawns)
        {
            var allColonyPawns = Find.CurrentMap?.mapPawns?.AllPawnsSpawned;
            if (allColonyPawns == null) return;

            foreach (Pawn otherPawn in allColonyPawns)
            {
                if (!processedPawns.Contains(otherPawn) && ShouldProcessPawn(pawn, otherPawn))
                {
                    processedPawns.Add(otherPawn);
                    AppendRelationInfo(pawn, otherPawn, sb);
                }
            }
        }

        /// <summary>
        /// 判断是否应该处理该 Pawn
        /// </summary>
        public static bool ShouldProcessPawn(Pawn pawn, Pawn otherPawn)
        {
            if (otherPawn == null || otherPawn == pawn) return false;
            if (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) return false;
            if (otherPawn.Dead) return false;
            if (otherPawn.relations is { hidePawnRelations: true }) return false;
            return true;
        }

        /// <summary>
        /// 追加关系信息到 StringBuilder
        /// </summary>
        public static void AppendRelationInfo(Pawn pawn, Pawn otherPawn, StringBuilder sb)
        {
            try
            {
                float opinionValue = pawn.relations.OpinionOf(otherPawn);
                string label = null;

                PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(otherPawn);
                if (mostImportantRelation != null)
                {
                    label = mostImportantRelation.GetGenderSpecificLabelCap(otherPawn);
                }

                if (string.IsNullOrEmpty(label))
                {
                    label = GetStatusLabel(pawn, otherPawn);
                }

                if (string.IsNullOrEmpty(label) && !pawn.IsVisitor() && !pawn.IsEnemy())
                {
                    if (opinionValue >= FriendOpinionThreshold)
                        label = "Friend".Translate();
                    else if (opinionValue <= RivalOpinionThreshold)
                        label = "Rival".Translate();
                    else
                        label = "Acquaintance".Translate();
                }

                if (!string.IsNullOrEmpty(label))
                {
                    string pawnName = otherPawn.LabelShort;
                    string opinion = opinionValue.ToStringWithSign();
                    sb.Append($"{pawnName}({label}) {opinion}, ");
                }
            }
            catch (Exception)
            {
                // Skip this pawn if opinion calculation fails
            }
        }

        /// <summary>
        /// 获取状态标签
        /// </summary>
        public static string GetStatusLabel(Pawn pawn, Pawn otherPawn)
        {
            if ((pawn.IsPrisoner || pawn.IsSlave) && otherPawn.IsFreeNonSlaveColonist)
                return "Master".Translate();

            if (otherPawn.IsPrisoner) return "Prisoner".Translate();
            if (otherPawn.IsSlave) return "Slave".Translate();

            if (pawn.Faction != null && otherPawn.Faction != null && pawn.Faction.HostileTo(otherPawn.Faction))
                return "Enemy".Translate();

            return null;
        }
    }
}