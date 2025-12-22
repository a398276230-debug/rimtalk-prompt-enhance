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
            this.promptText = currentPrompt;
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
            Widgets.Label(tipRect, "提示：留空将使用默认提示词。支持多行文本。\n可用变量：{overview} (概况), {diffReport} (变化), {actions} (操作), {events} (事件)");
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
            if (Widgets.ButtonText(new Rect(0f, buttonY, buttonWidth, 35f), "恢复默认"))
            {
                promptText = "";
                Messages.Message("已清空自定义提示词，将使用默认提示词", MessageTypeDefOf.PositiveEvent, false);
            }

            // 查看默认按钮
            if (Widgets.ButtonText(new Rect(buttonWidth + gap, buttonY, buttonWidth, 35f), "查看默认"))
            {
                Find.WindowStack.Add(new Dialog_MessageBox(
                    $"默认提示词：\n\n{defaultPrompt}",
                    "关闭", null, null, null, null, false, null, null
                ));
            }

            // 保存按钮
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth * 2 - gap, buttonY, buttonWidth, 35f), "保存"))
            {
                onSave?.Invoke(promptText);
                Messages.Message("提示词已保存", MessageTypeDefOf.PositiveEvent, false);
                Close();
            }

            // 取消按钮
            if (Widgets.ButtonText(new Rect(inRect.width - buttonWidth, buttonY, buttonWidth, 35f), "取消"))
            {
                Close();
            }

            // 字符计数
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            string countText = string.IsNullOrEmpty(promptText) 
                ? "使用默认提示词" 
                : $"字符数: {promptText.Length}";
            Widgets.Label(new Rect(0f, buttonY + 40f, inRect.width, 20f), countText);
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}
