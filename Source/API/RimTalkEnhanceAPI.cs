using System;
using RimTalk.API;
using RimTalkHealthEnhance.API.Entries;
using RimTalkHealthEnhance.API.Hooks;
using RimTalkHealthEnhance.API.Variables;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// RimTalk 增强提示词 API 注册服务
    /// 使用 RimTalk 官方 API 注册 hooks、变量和 entries
    /// </summary>
    public static class RimTalkEnhanceAPI
    {
        private const string MOD_ID = "RimTalkHealthEnhance";
        private static bool _registered = false;
        private static string _colonyStatusEntryId = null;

        /// <summary>
        /// 在 Mod 初始化时调用，注册所有 API hooks
        /// </summary>
        public static void Initialize()
        {
            if (_registered) return;

            try
            {
                // 注册 Pawn Hooks
                PawnHookRegistrar.Register(MOD_ID);

                // 注册上下文变量
                ContextVariableRegistrar.Register(MOD_ID);

                // 注册 Prompt Entries
                _colonyStatusEntryId = PromptEntryRegistrar.Register(MOD_ID);

                _registered = true;
                Log.Message("[RimTalk Enhance] API hooks registered successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to register API hooks: {ex}");
                // 如果 API 注册失败，Harmony patches 仍然作为备用方案
            }
        }

        /// <summary>
        /// Mod 卸载时调用，清理所有注册
        /// </summary>
        public static void Cleanup()
        {
            if (!_registered) return;

            try
            {
                RimTalkPromptAPI.UnregisterAllHooks(MOD_ID);

                // 移除 Prompt Entry
                if (!string.IsNullOrEmpty(_colonyStatusEntryId))
                {
                    RimTalkPromptAPI.RemovePromptEntry(_colonyStatusEntryId);
                }

                _registered = false;
                Log.Message("[RimTalk Enhance] API hooks unregistered");
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhance] Error during cleanup: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查 API 是否已注册
        /// </summary>
        public static bool IsRegistered => _registered;

        /// <summary>
        /// 获取 Mod ID
        /// </summary>
        internal static string ModId => MOD_ID;
    }
}