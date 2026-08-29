using RimTalk.API;
using Verse;

namespace RimTalkHealthEnhance.API.Variables
{
    /// <summary>
    /// 负责注册所有上下文变量 (Mustache Variables)
    /// </summary>
    internal static class ContextVariableRegistrar
    {
        /// <summary>
        /// 注册所有上下文变量
        /// </summary>
        public static void Register(string modId)
        {
            RegisterColonyStatusVariable(modId);
            RegisterColonyHistoryVariable(modId);
            RegisterColonyLayoutVariable(modId);
            RegisterColonyFactionsVariable(modId);
            RegisterLocalSocialVariable(modId);
        }

        /// <summary>
        /// 注册殖民地状况变量 {{colony_status}}
        /// </summary>
        private static void RegisterColonyStatusVariable(string modId)
        {
            RimTalkPromptAPI.RegisterContextVariable(
                modId,
                "colony_status",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.ShowColonyAnnouncements)
                    {
                        DebugLog.Dump("Variable {{colony_status}}", "(disabled: ShowColonyAnnouncements=off)");
                        return "";
                    }

                    var result = AnnouncementBuilder.BuildAnnouncementContext() ?? "";
                    DebugLog.Dump("Variable {{colony_status}}", result);
                    return result;
                },
                description: "RTE_API_ColonyStatus_Desc".Translate(),
                priority: 100
            );
        }

        /// <summary>
        /// 注册殖民地历史变量 {{colony_history}}
        /// </summary>
        private static void RegisterColonyHistoryVariable(string modId)
        {
            RimTalkPromptAPI.RegisterContextVariable(
                modId,
                "colony_history",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.EnableAISynthesis || !settings.InjectSnapshotToContext)
                    {
                        DebugLog.Dump("Variable {{colony_history}}", "(disabled: EnableAISynthesis or InjectSnapshotToContext=off)");
                        return "";
                    }

                    var result = ColonyHistoryContextBuilder.Build() ?? "";
                    DebugLog.Dump("Variable {{colony_history}}", result);
                    return result;
                },
                description: "RTE_API_ColonyHistory_Desc".Translate(),
                priority: 100
            );
        }

        /// <summary>
        /// 注册殖民地布局变量 {{colony_layout}}
        /// </summary>
        private static void RegisterColonyLayoutVariable(string modId)
        {
            RimTalkPromptAPI.RegisterContextVariable(
                modId,
                "colony_layout",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.EnableGlobalLayout)
                    {
                        DebugLog.Dump("Variable {{colony_layout}}", "(disabled: EnableGlobalLayout=off)");
                        return "";
                    }

                    // 对话实际发生地图（ctx.Map，来自 PromptContext.FromTalkRequest），非玩家当前查看地图
                    var map = ctx.Map;
                    if (map == null || !map.IsPlayerHome)
                    {
                        DebugLog.Dump("Variable {{colony_layout}}", "(skipped: no current map or not player home)");
                        return "";
                    }

                    var result = ColonyLayoutBuilder.GetColonyLayout(map) ?? "";
                    DebugLog.Dump("Variable {{colony_layout}}", result);
                    return result;
                },
                description: "RTE_API_ColonyLayout_Desc".Translate(),
                priority: 100
            );
        }

        /// <summary>
        /// 注册派系信息变量 {{colony_factions}}
        /// </summary>
        private static void RegisterColonyFactionsVariable(string modId)
        {
            RimTalkPromptAPI.RegisterContextVariable(
                modId,
                "colony_factions",
                (ctx) =>
                {
                    var settings = RimTalkHealthEnhanceMod.Settings;
                    if (!settings.ShowFactionRelations)
                    {
                        DebugLog.Dump("Variable {{colony_factions}}", "(disabled: ShowFactionRelations=off)");
                        return "";
                    }

                    var result = FactionInfoBuilder.BuildFactionContext() ?? "";
                    DebugLog.Dump("Variable {{colony_factions}}", result);
                    return result;
                },
                description: "RTE_API_ColonyFactions_Desc".Translate(),
                priority: 100
            );
        }

        /// <summary>
        /// 注册本地图社交关系变量 {{local_social}}
        /// 只获取当前地图的所有 pawn 社交关系，不包含世界 pawns
        /// </summary>
        private static void RegisterLocalSocialVariable(string modId)
        {
            RimTalkPromptAPI.RegisterPawnVariable(
                modId,
                "local_social",
                (pawn) =>
                {
                    var result = LocalSocialBuilder.GetLocalMapSocialString(pawn);
                    DebugLog.Dump($"PawnVariable[{{{pawn?.LabelShort ?? "null"}}}] {{{{local_social}}}}", result);
                    return result;
                },
                description: "RTE_API_LocalSocial_Desc".Translate(),
                priority: 100
            );
        }

    }
}