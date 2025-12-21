using RimTalk.Service;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 调试辅助工具 - 用于验证健康信息是否正确显示在prompt中
    /// </summary>
    public static class DebugHelper
    {
        /// <summary>
        /// 在开发者模式下，将pawn的完整上下文信息输出到日志
        /// </summary>
        public static void LogPawnContext(Pawn pawn)
        {
            if (!Prefs.DevMode) return;

            var context = PromptService.CreatePawnContext(pawn, PromptService.InfoLevel.Normal);
            
            Log.Message("=== RimTalk Enhanced Health - Pawn Context ===");
            Log.Message($"Pawn: {pawn.LabelShort}");
            Log.Message("--- Full Context ---");
            Log.Message(context);
            Log.Message("=== End Context ===");
        }

        /// <summary>
        /// 测试健康信息构建器
        /// </summary>
        public static void TestHealthInfo(Pawn pawn)
        {
            if (!Prefs.DevMode) return;

            var healthInfo = HealthInfoBuilder.BuildEnhancedHealthContext(pawn, PromptService.InfoLevel.Normal);
            
            Log.Message("=== RimTalk Enhanced Health - Health Info Test ===");
            Log.Message($"Pawn: {pawn.LabelShort}");
            Log.Message("--- Health Info ---");
            Log.Message(healthInfo ?? "(No health issues)");
            Log.Message("=== End Test ===");
        }
    }
}
