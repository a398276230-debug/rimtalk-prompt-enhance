using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    public static class ResearchInfoBuilder
    {
        public static string BuildResearchContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.IncludeResearchInSnapshot)
                return null;
            
            var researchManager = Find.ResearchManager;
            if (researchManager == null)
                return null;
            
            var sb = new StringBuilder();
            
            // 当前正在研究的项目
            var currentProj = researchManager.GetProject();
            if (currentProj != null)
            {
                float progress = researchManager.GetProgress(currentProj);
                float total = currentProj.baseCost;
                int percentage = (int)((progress / total) * 100);
                
                sb.AppendLine($"Current Research: {currentProj.LabelCap} ({percentage}%)");
            }
            
            // 获取所有科技项目
            var allProjects = DefDatabase<ResearchProjectDef>.AllDefsListForReading;
            
            // 已完成的科技
            var completed = allProjects
                .Where(p => p.IsFinished)
                .Select(p => p.LabelCap)
                .ToList();
            
            if (completed.Any())
            {
                sb.AppendLine($"\nCompleted Research ({completed.Count}):");
                sb.AppendLine(string.Join(", ", completed));
            }
            
            // 未完成的科技（可选，根据设置）
            if (settings.IncludeUnfinishedResearch)
            {
                var unfinished = allProjects
                    .Where(p => !p.IsFinished && p != currentProj)
                    .Select(p => p.LabelCap)
                    .ToList();
                
                if (unfinished.Any())
                {
                    sb.AppendLine($"\nAvailable Research ({unfinished.Count}):");
                    sb.AppendLine(string.Join(", ", unfinished));
                }
            }
            
            return sb.ToString();
        }
    }
}
