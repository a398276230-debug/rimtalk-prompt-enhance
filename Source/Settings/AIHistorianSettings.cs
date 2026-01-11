using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// AI 提供商枚举
    /// </summary>
    public enum AIProvider
    {
        OpenAI,
        Google, // Gemini
        DeepSeek,
        Player2,
        Custom // OpenAI Compatible
    }

    /// <summary>
    /// 快照注入模式枚举
    /// </summary>
    public enum SnapshotInjectionMode
    {
        Context,  // 注入到 Context（系统上下文）
        Prompt    // 注入到 Prompt（对话提示词）
    }

    /// <summary>
    /// AI 史官相关设置
    /// </summary>
    public class AIHistorianSettings : IExposable
    {
        // === 基础设置 ===
        public bool EnableAISynthesis = false;
        public bool InjectSnapshotToContext = true;      // 是否将快照注入到 AI context
        public SnapshotInjectionMode SnapshotInjectionTarget = SnapshotInjectionMode.Context; // 注入位置
        public float SnapshotInjectDays = 1.0f;          // 注入多少天的快照（0.5-7天）
        
        // === 内容包含设置 ===
        public bool IncludeProjectsInSnapshot = true;    // 将状况板工程信息发给史官
        public bool IncludeResearchInSnapshot = false;   // 将科技状态发给史官（默认关闭）
        public bool IncludeUnfinishedResearch = false;   // 包含未完成的科技列表
        public bool IncludePowerInSnapshot = false;      // 将电力状态发给史官（默认关闭）
        
        // === API 配置 ===
        public AIProvider SynthesisProvider = AIProvider.OpenAI;
        public string CustomApiKey = "";
        public string CustomApiUrl = "";
        public string CustomModelName = "gpt-4o-mini";
        
        // === 自定义提示词 ===
        public string CustomOverviewSummaryPrompt = "";  // 概况总结提示词
        public string CustomDailySynthesisPrompt = "";   // 每日快照提示词
        public string CustomProjectSummaryPrompt = "";   // 工程AI总结提示词

        public void ExposeData()
        {
            Scribe_Values.Look(ref EnableAISynthesis, "enableAISynthesis", false);
            Scribe_Values.Look(ref InjectSnapshotToContext, "injectSnapshotToContext", true);
            Scribe_Values.Look(ref SnapshotInjectionTarget, "snapshotInjectionTarget", SnapshotInjectionMode.Context);
            Scribe_Values.Look(ref SnapshotInjectDays, "snapshotInjectDays", 1.0f);
            Scribe_Values.Look(ref IncludeProjectsInSnapshot, "includeProjectsInSnapshot", true);
            Scribe_Values.Look(ref IncludeResearchInSnapshot, "includeResearchInSnapshot", false);
            Scribe_Values.Look(ref IncludeUnfinishedResearch, "includeUnfinishedResearch", false);
            Scribe_Values.Look(ref IncludePowerInSnapshot, "includePowerInSnapshot", false);
            Scribe_Values.Look(ref SynthesisProvider, "synthesisProvider", AIProvider.OpenAI);
            Scribe_Values.Look(ref CustomApiKey, "customApiKey", "");
            Scribe_Values.Look(ref CustomApiUrl, "customApiUrl", "");
            Scribe_Values.Look(ref CustomModelName, "customModelName", "gpt-4o-mini");
            Scribe_Values.Look(ref CustomOverviewSummaryPrompt, "customOverviewSummaryPrompt", "");
            Scribe_Values.Look(ref CustomDailySynthesisPrompt, "customDailySynthesisPrompt", "");
            Scribe_Values.Look(ref CustomProjectSummaryPrompt, "customProjectSummaryPrompt", "");
        }

        /// <summary>
        /// 获取指定提供商的默认 API URL
        /// </summary>
        public static string GetDefaultUrl(AIProvider provider)
        {
            switch (provider)
            {
                case AIProvider.OpenAI: return "https://api.openai.com/v1/chat/completions";
                case AIProvider.Google: return "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent";
                case AIProvider.DeepSeek: return "https://api.deepseek.com/v1/chat/completions";
                case AIProvider.Player2: return "https://api.player2.game/v1/chat/completions";
                default: return "";
            }
        }

        /// <summary>
        /// 绘制 AI 史官设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_AI_Enable".Translate(), ref EnableAISynthesis, "RTE_Settings_AI_Enable_Desc".Translate());
            
            if (EnableAISynthesis)
            {
                listing.Gap();
                
                Rect providerRect = listing.GetRect(30f);
                Widgets.Label(providerRect.LeftHalf(), "RTE_Settings_AI_Provider".Translate());
                if (Widgets.ButtonText(providerRect.RightHalf(), SynthesisProvider.ToString()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>
                    {
                        new FloatMenuOption("OpenAI", () => { SynthesisProvider = AIProvider.OpenAI; CustomModelName = "gpt-4o-mini"; }),
                        new FloatMenuOption("Google (Gemini)", () => { SynthesisProvider = AIProvider.Google; CustomModelName = "gemini-2.5-flash"; }),
                        new FloatMenuOption("DeepSeek", () => { SynthesisProvider = AIProvider.DeepSeek; CustomModelName = "deepseek-chat"; }),
                        new FloatMenuOption("Player2", () => { SynthesisProvider = AIProvider.Player2; CustomModelName = ""; CustomApiKey = ""; }),
                        new FloatMenuOption("Custom (OpenAI Compatible)", () => SynthesisProvider = AIProvider.Custom)
                    };
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_APIConfig".Translate());
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_APIKey".Translate());
                CustomApiKey = listing.TextEntry(CustomApiKey);
                
                string defaultUrl = GetDefaultUrl(SynthesisProvider);
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_APIURL".Translate(SynthesisProvider));
                CustomApiUrl = listing.TextEntry(CustomApiUrl);
                if (string.IsNullOrEmpty(CustomApiUrl) && !string.IsNullOrEmpty(defaultUrl))
                {
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AI_DefaultURL".Translate(defaultUrl));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_ModelName".Translate());
                CustomModelName = listing.TextEntry(CustomModelName);
                
                listing.Gap();
                
                if (listing.ButtonText("RTE_Settings_AI_TestConnection".Translate()))
                {
                    System.Threading.Tasks.Task.Run(async () => 
                    {
                        string result = await SimpleAIClient.CallAI("Hello, are you there?");
                        if (!string.IsNullOrEmpty(result))
                            Messages.Message("RTE_Settings_AI_TestSuccess".Translate(result), MessageTypeDefOf.PositiveEvent, false);
                        else
                            Messages.Message("RTE_Settings_AI_TestFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                    });
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_SnapshotContent".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludeProjects".Translate(), ref IncludeProjectsInSnapshot,
                    "RTE_Settings_AI_IncludeProjects_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludeResearch".Translate(), ref IncludeResearchInSnapshot,
                    "RTE_Settings_AI_IncludeResearch_Desc".Translate());
                
                if (IncludeResearchInSnapshot)
                {
                    listing.CheckboxLabeled("RTE_Settings_AI_IncludeUnfinished".Translate(), ref IncludeUnfinishedResearch,
                        "RTE_Settings_AI_IncludeUnfinished_Desc".Translate());
                }
                
                listing.CheckboxLabeled("RTE_Settings_AI_IncludePower".Translate(), ref IncludePowerInSnapshot,
                    "RTE_Settings_AI_IncludePower_Desc".Translate());
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Widgets.Label(listing.GetRect(24f), "RTE_Settings_AI_ContextInjection".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_AI_InjectSnapshot".Translate(), ref InjectSnapshotToContext, 
                    "RTE_Settings_AI_InjectSnapshot_Desc".Translate());
                
                if (InjectSnapshotToContext)
                {
                    // 注入位置选择
                    Rect modeRect = listing.GetRect(30f);
                    Widgets.Label(modeRect.LeftHalf(), "RTE_Settings_AI_InjectionMode".Translate());
                    if (Widgets.ButtonText(modeRect.RightHalf(), 
                        SnapshotInjectionTarget == SnapshotInjectionMode.Context ? "RTE_Settings_AI_InjectionMode_Context".Translate() : "RTE_Settings_AI_InjectionMode_Prompt".Translate()))
                    {
                        List<FloatMenuOption> options = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("RTE_Settings_AI_InjectionMode_Context".Translate(), () => SnapshotInjectionTarget = SnapshotInjectionMode.Context),
                            new FloatMenuOption("RTE_Settings_AI_InjectionMode_Prompt".Translate(), () => SnapshotInjectionTarget = SnapshotInjectionMode.Prompt)
                        };
                        Find.WindowStack.Add(new FloatMenu(options));
                    }
                    
                    // 说明文字
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    if (SnapshotInjectionTarget == SnapshotInjectionMode.Context)
                    {
                        Widgets.Label(listing.GetRect(36f), "RTE_Settings_AI_InjectionMode_Context_Desc".Translate());
                    }
                    else
                    {
                        Widgets.Label(listing.GetRect(36f), "RTE_Settings_AI_InjectionMode_Prompt_Desc".Translate());
                    }
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;

                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_AI_InjectDays".Translate(SnapshotInjectDays.ToString("F1")));
                    SnapshotInjectDays = listing.Slider(SnapshotInjectDays, 0.5f, 7f);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_AI_InjectDays_Desc".Translate(SnapshotInjectDays.ToString("F1")));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
                
                listing.Gap();
                listing.GapLine();
                listing.Gap();
                
                Text.Font = GameFont.Tiny;
                Widgets.Label(listing.GetRect(120f), 
                    "说明：\n" +
                    "1. 每日 0 点系统会自动拍摄殖民地快照（建筑、房间、蓝图）。\n" +
                    "2. AI 将对比昨日快照，结合玩家操作日志、工程进度、科技状态和事件，生成一段简短的总结。\n" +
                    "3. 总结结果将显示在'每日快照'标签页中，不会直接修改概况。\n" +
                    "4. 如果启用'自动注入'，AI 在对话时会自动看到最近的历史记录（含日期）。\n" +
                    "5. 工程信息：从状况板读取进行中和已完成的工程项目。\n" +
                    "6. 科技状态：包含当前研究、已完成科技，可选包含未完成科技（默认关闭以节省token）。");
                Text.Font = GameFont.Small;
            }
        }
    }
}