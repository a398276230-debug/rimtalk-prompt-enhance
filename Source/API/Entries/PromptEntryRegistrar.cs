using System;
using System.Text.RegularExpressions;
using RimTalk.API;
using RimTalk.Prompt;
using Verse;

namespace RimTalkHealthEnhance.API.Entries
{
    /// <summary>
    /// 负责注册所有 Prompt Entries
    /// </summary>
    internal static class PromptEntryRegistrar
    {
        private const string COLONY_STATUS_ENTRY_NAME = "Colony Status (RimTalk Enhance)";

        /// <summary>
        /// 注册所有 Prompt Entries，返回殖民地状况条目的 ID
        /// </summary>
        public static string Register(string modId)
        {
            return RegisterColonyStatusEntry(modId);
        }

        /// <summary>
        /// 获取确定性的 Entry ID（用于去重和移除）
        /// 格式：mod_{sanitizedModId}_{sanitizedName}
        /// </summary>
        public static string GetDeterministicEntryId(string modId, string name)
        {
            var sanitizedModId = SanitizeForId(modId);
            var sanitizedName = SanitizeForId(name);
            return $"mod_{sanitizedModId}_{sanitizedName}";
        }

        private static string SanitizeForId(string input)
        {
            if (string.IsNullOrEmpty(input)) return "unknown";
            return Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]", "");
        }

        /// <summary>
        /// 获取当前 Entry 的内容（用于更新）
        /// </summary>
        private static string GetColonyStatusEntryContent()
        {
            return "{{colony_status}}\n{{colony_history}}\n{{colony_layout}}\n{{colony_factions}}";
        }

        /// <summary>
        /// 注册殖民地状况 Prompt Entry
        /// </summary>
        private static string RegisterColonyStatusEntry(string modId)
        {
            var entryId = GetDeterministicEntryId(modId, COLONY_STATUS_ENTRY_NAME);

            // 首先检查是否已存在该条目，如果存在则更新内容（确保 Mod 更新后内容同步到玩家预设）
            try
            {
                var preset = RimTalkPromptAPI.GetActivePreset();
                if (preset != null)
                {
                    var existingEntry = preset.GetEntry(entryId);
                    if (existingEntry != null)
                    {
                        // 条目已存在 → 直接更新 Content
                        existingEntry.Content = GetColonyStatusEntryContent();
                        Log.Message($"[RimTalk Enhance] ✓ Updated existing PromptEntry: {COLONY_STATUS_ENTRY_NAME}");
                        return entryId;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Message($"[RimTalk Enhance] Failed to check/update existing entry: {ex.Message}");
            }

            // 创建殖民地状况 Entry，包含所有胡子变量
            var entry = RimTalkPromptAPI.CreatePromptEntry(
                name: COLONY_STATUS_ENTRY_NAME,
                content: GetColonyStatusEntryContent(),
                role: PromptRole.System,
                position: PromptPosition.Relative,
                inChatDepth: 0,
                sourceModId: modId
            );

            // 插入到 "Chat History" 之前（即 "Pawn Profiles" 之后，第四位）
            if (RimTalkPromptAPI.InsertPromptEntryBeforeName(entry, "Chat History"))
            {
                Log.Message("[RimTalk Enhance] Colony status entry inserted before Chat History");
            }
            else
            {
                Log.Message("[RimTalk Enhance] Chat History not found, colony status entry added at end");
            }

            return entryId;
        }
    }
}