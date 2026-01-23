using System.Collections.Generic;
using System.Linq;
using RimTalk.Service;
using RimTalk.Util;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance.API
{
    /// <summary>
    /// 构建特质上下文信息（无限制版本）
    /// </summary>
    internal static class TraitsContextBuilder
    {
        /// <summary>
        /// 获取无限制的特质字符串
        /// </summary>
        public static string GetTraitsContextUnlimited(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            var traits = new List<string>();

            foreach (var trait in pawn.story?.traits?.TraitsSorted ?? Enumerable.Empty<Trait>())
            {
                var degreeData = trait.def.degreeDatas?.FirstOrDefault(d => d.degree == trait.Degree);
                if (degreeData != null)
                {
                    var traitText = infoLevel == PromptService.InfoLevel.Full
                        ? $"{degreeData.label}:{CommonUtil.Sanitize(degreeData.description, pawn)}"
                        : degreeData.label;
                    traits.Add(traitText);
                }
            }

            if (traits.Any())
            {
                var separator = infoLevel == PromptService.InfoLevel.Full ? "\n" : ",";
                return $"Traits: {string.Join(separator, traits)}";
            }

            return null;
        }
    }
}