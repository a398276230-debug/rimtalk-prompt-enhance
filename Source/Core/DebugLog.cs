using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 调试日志网关：仅当 mod 设置中开启 Debug 模式时才输出。
    /// 所有运行期（游戏过程中反复触发的）日志都应走这里；
    /// 加载期一次性验证日志（hooks registered / patches applied 等）保持常开。
    /// </summary>
    public static class DebugLog
    {
        /// <summary>Debug 模式是否开启（设置未加载时安全返回 false）</summary>
        public static bool Enabled => RimTalkHealthEnhanceMod.Settings?.Misc?.DebugMode ?? false;

        /// <summary>输出一条调试日志（自动加前缀）</summary>
        public static void Log(string message)
        {
            if (!Enabled) return;
            Verse.Log.Message($"[RimTalk Enhance][Debug] {message}");
        }

        /// <summary>
        /// 输出一段采集到的上下文数据（用于检验各类信息收集内容）。
        /// 空内容输出 "(empty)" 以便发现"本应有数据但为空"的问题。
        /// </summary>
        public static void Dump(string title, string content)
        {
            if (!Enabled) return;
            Verse.Log.Message($"=== [RimTalk Enhance][Debug] {title} ===");
            Verse.Log.Message(string.IsNullOrEmpty(content) ? "(empty)" : content);
            Verse.Log.Message($"=== [RimTalk Enhance][Debug] End {title} ({(string.IsNullOrEmpty(content) ? 0 : content.Length)} chars) ===");
        }
    }
}
