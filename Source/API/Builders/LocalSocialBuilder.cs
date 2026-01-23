using System;
using System.Linq;
using System.Text;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.API
{
    /// <summary>
    /// 构建本地地图社交关系信息
    /// </summary>
    internal static class LocalSocialBuilder
    {
        private const float FriendOpinionThreshold = 20f;
        private const float RivalOpinionThreshold = -20f;

        /// <summary>
        /// 获取本地图所有 pawn 的社交关系字符串
        /// 只包含当前地图的 pawns，不包含世界 pawns
        /// </summary>
        public static string GetLocalMapSocialString(Pawn pawn)
        {
            if (pawn?.relations == null || pawn.Map == null) return "";

            var localPawns = pawn.Map.mapPawns?.AllPawnsSpawned;
            if (localPawns == null) return "";

            var relationsSb = new StringBuilder();
            foreach (var otherPawn in localPawns.OrderBy(p => p.LabelShort))
            {
                if (otherPawn == null || otherPawn == pawn) continue;
                if (!otherPawn.RaceProps.Humanlike && !otherPawn.HasVocalLink()) continue;
                if (otherPawn.Dead) continue;
                if (otherPawn.relations is { hidePawnRelations: true }) continue;

                if (TryGetLocalSocialLabel(pawn, otherPawn, out var label))
                {
                    string pawnName = otherPawn.LabelShort;
                    relationsSb.Append($"{pawnName}({label}), ");
                }
            }

            if (relationsSb.Length > 0)
            {
                relationsSb.Length -= 2; // Remove trailing ", "
                return relationsSb.ToString();
            }

            return "";
        }

        /// <summary>
        /// 尝试获取社交标签（用于本地图社交）
        /// </summary>
        private static bool TryGetLocalSocialLabel(Pawn pawn, Pawn otherPawn, out string label)
        {
            label = null;
            float opinionValue = 0f;

            try
            {
                opinionValue = pawn.relations.OpinionOf(otherPawn);
            }
            catch (Exception)
            {
                return false;
            }

            // Step 1: Check for the most important direct or family relationship
            PawnRelationDef mostImportantRelation = pawn.GetMostImportantRelation(otherPawn);
            if (mostImportantRelation != null)
            {
                label = mostImportantRelation.GetGenderSpecificLabelCap(otherPawn);
            }

            // Step 2: If no family relation, check for an overriding status
            if (string.IsNullOrEmpty(label))
            {
                label = RelationsContextBuilder.GetStatusLabel(pawn, otherPawn);
            }

            // Step 3: If no other label found, fall back to opinion-based relationship
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

            return !string.IsNullOrEmpty(label);
        }
    }
}