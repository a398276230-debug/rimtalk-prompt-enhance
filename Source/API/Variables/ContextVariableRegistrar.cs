using RimTalk.API;
using RimTalk.Util;
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
            RegisterMapWealthVariable(modId);
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
                    if (!settings.ShowColonyAnnouncements) return "";

                    return AnnouncementBuilder.BuildAnnouncementContext() ?? "";
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
                        return "";

                    return ColonyHistoryContextBuilder.Build() ?? "";
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
                    if (!settings.EnableGlobalLayout) return "";

                    var map = Find.CurrentMap;
                    if (map == null || !map.IsPlayerHome) return "";

                    return ColonyLayoutBuilder.GetColonyLayout(map) ?? "";
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
                    if (!settings.ShowFactionRelations) return "";

                    return FactionInfoBuilder.BuildFactionContext() ?? "";
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
                (pawn) => LocalSocialBuilder.GetLocalMapSocialString(pawn),
                description: "RTE_API_LocalSocial_Desc".Translate(),
                priority: 100
            );
        }

        /// <summary>
        /// 注册地图财富等级变量 {{map.wealth}}
        /// 使用RimTalk的Describer.Wealth方法返回财富分级描述
        /// </summary>
        private static void RegisterMapWealthVariable(string modId)
        {
            RimTalkPromptAPI.RegisterEnvironmentVariable(
                modId,
                "wealth",
                (map) =>
                {
                    if (map == null) return "";

                    float wealthTotal = map.wealthWatcher.WealthTotal;
                    return Describer.Wealth(wealthTotal);
                },
                description: "RTE_API_MapWealth_Desc".Translate(),
                priority: 100
            );
        }
    }
}