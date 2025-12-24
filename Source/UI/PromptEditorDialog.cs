using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public class PromptEditorDialog : Window
    {
        private string promptText;
        private string defaultPrompt;
        private System.Action<string> onSave;
        private string title;
        private Vector2 scrollPosition;

        public override Vector2 InitialSize => new Vector2(800f, 600f);

        public PromptEditorDialog(string title, string currentPrompt, string defaultPrompt, System.Action<string> onSave)
        {
            this.title = title;
            // 如果当前提示词为空，则预填充默认提示词，方便玩家修改
            this.promptText = string.IsNullOrEmpty(currentPrompt) ? defaultPrompt : currentPrompt;
            this.defaultPrompt = defaultPrompt;
            this.onSave = onSave;
            this.doCloseX = true;
            this.forcePause = true;
            this.absorbInputAroundWindow = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(0f, 0f, inRect.width, 35f), title);
            Text.Font = GameFont.Small;

            // 提示信息
            Rect tipRect = new Rect(0f, 40f, inRect.width, 40f);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.yellow;
            Widgets.Label(tipRect, "RTE_PromptEditor_Tip".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            // 文本编辑区域
            Rect textAreaRect = new Rect(0f, 85f, inRect.width, inRect.height - 170f);
            Widgets.DrawMenuSection(textAreaRect);
            
            Rect innerRect = textAreaRect.ContractedBy(10f);
            
            // 使用 BeginScrollView + TextArea 代替 TextAreaScrollable
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, Mathf.Max(innerRect.height, Text.CalcHeight(promptText, innerRect.width - 16f) + 10f));
            Widgets.BeginScrollView(innerRect, ref scrollPosition, viewRect);
            promptText = Widgets.TextArea(viewRect, promptText);
            Widgets.EndScrollView();

            // 按钮区域
            float buttonY = inRect.height - 75f;
            float buttonWidth = 120f;
            float gap = 10f;

            // 恢复默认按钮
            if (Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, 35f), "RTE_PromptEditor_RestoreDefault".Translate()))
            {
                promptText = "";
                Messages.Message("RTE_PromptEditor_Restored".Translate(), MessageTypeDefOf.PositiveEvent, false);
            }

            // 查看默认按钮
            if (Widgets.ButtonText(new Rect(buttonWidth + gap, buttonY, buttonWidth, 35f), "RTE_PromptEditor_ViewDefault".Translate()))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    "RTE_PromptEditor_DefaultPrompt".Translate(defaultPrompt),
                    "RTE_PromptEditor_Close".Translate(), null, null, null, null, false, null, null
                ));
            }

            // 保存按钮
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth * 2 - gap, buttonY, buttonWidth, 35f), "RTE_PromptEditor_Save".Translate()))
            {
                onSave?.Invoke(promptText);
                Messages.Message("RTE_PromptEditor_Saved".Translate(), MessageTypeDefOf.PositiveEvent, false);
                Close();
            }

            // 取消按钮
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 35f), "RTE_PromptEditor_Cancel".Translate()))
            {
                Close();
            }

            // 字符计数
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            string countText = string.IsNullOrEmpty(promptText) 
                ? "RTE_PromptEditor_UseDefault".Translate() 
                : "RTE_PromptEditor_CharCount".Translate(promptText.Length);
            Widgets.Label(new Rect(0f, buttonY + 40f, inRect.width, 20f), countText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
