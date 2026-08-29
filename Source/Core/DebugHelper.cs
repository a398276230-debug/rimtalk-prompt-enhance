using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 调试辅助工具 - 用于验证健康信息是否正确显示在prompt中
    /// 受 mod 设置 Debug 模式控制（也可通过 DebugAction 快速开关）
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// 将pawn的完整上下文信息输出到日志
        /// </summary>
        public static void LogPawnContext(Pawn pawn)
        {
            if (!DebugLog.Enabled || pawn == null) return;

            var context = PromptService.CreatePawnContext(pawn, PromptService.InfoLevel.Normal);

            DebugLog.Dump($"FullPawnContext[{pawn.LabelShort}]", context);
        }

        /// <summary>
        /// 测试健康信息构建器
        /// </summary>
        public static void TestHealthInfo(Pawn pawn)
        {
            if (!DebugLog.Enabled || pawn == null) return;

            var healthInfo = HealthInfoBuilder.BuildEnhancedHealthContext(pawn, PromptService.InfoLevel.Normal);

            DebugLog.Dump($"HealthInfoTest[{pawn.LabelShort}]", healthInfo);
        }
    }
}
