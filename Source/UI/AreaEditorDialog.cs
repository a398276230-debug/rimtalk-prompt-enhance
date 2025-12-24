using RimTalkHealthEnhance.Models;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance.UI
{
    /// <summary>
    /// 自定义区域编辑对话框
    /// </summary>
    public class AreaEditorDialog : Window
    {
        private CustomNamedArea area;
        private string labelBuffer;
        private bool isNew;
        
        public override Vector2 InitialSize => new Vector2(400f, 300f);
        
        public AreaEditorDialog(CustomNamedArea area, bool isNew = false)
        {
            this.area = area;
            this.isNew = isNew;
            this.labelBuffer = area.Label;
            
            doCloseButton = false; // 使用默认关闭按钮
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // 区域名称
            Text.Font = GameFont.Small;
            Widgets.Label(listing.GetRect(22f), "RTE_AreaEditor_AreaName".Translate());
            labelBuffer = listing.TextEntry(labelBuffer);
            
            listing.Gap();
            
            // 颜色选择
            Widgets.Label(listing.GetRect(22f), "RTE_AreaEditor_AreaColor".Translate());
            Rect colorRect = listing.GetRect(30f);
            Rect colorBoxRect = colorRect.LeftHalf().ContractedBy(2f);
            Rect colorButtonRect = colorRect.RightHalf().ContractedBy(2f);
            
            Widgets.DrawBoxSolid(colorBoxRect, area.Color);
            if (Widgets.ButtonText(colorButtonRect, "RTE_AreaEditor_SelectColor".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ColorPicker(area.Color, (newColor) => {
                    area.Color = newColor;
                }));
            }
            
            listing.Gap();
            
            // 启用/禁用
            listing.CheckboxLabeled("RTE_AreaEditor_EnableArea".Translate(), ref area.IsActive);
            
            listing.Gap();
            
            // 统计信息
            Widgets.Label(listing.GetRect(22f), "RTE_AreaEditor_CellCount_Display".Translate(area.CellCount));
            
            listing.Gap();
            
            listing.Gap();
            listing.Gap();
            
            // 操作按钮 - 左右布局
            Rect buttonRow = listing.GetRect(35f);
            Rect saveButton = buttonRow.LeftHalf().ContractedBy(2f);
            Rect clearButton = buttonRow.RightHalf().ContractedBy(2f);
            
            if (Widgets.ButtonText(saveButton, "RTE_AreaEditor_SaveAndClose".Translate()))
            {
                area.Label = labelBuffer;
                var manager = ColonyAnnouncementManager.Instance;
                if (manager != null)
                    manager.NotifyDataChanged();
                Close();
            }
            
            if (Widgets.ButtonText(clearButton, "RTE_AreaEditor_ClearArea".Translate()))
            {
                area.Clear();
                var manager = ColonyAnnouncementManager.Instance;
                if (manager != null)
                    manager.NotifyDataChanged();
            }
            
            listing.End();
        }
        
        public override void OnAcceptKeyPressed()
        {
            // 回车键保存并关闭
            area.Label = labelBuffer;
            var manager = ColonyAnnouncementManager.Instance;
            if (manager != null)
                manager.NotifyDataChanged();
            base.OnAcceptKeyPressed();
        }
    }
    
    /// <summary>
    /// 颜色选择器对话框
    /// </summary>
    public class Dialog_ColorPicker : Window
    {
        private Color selectedColor;
        private System.Action<Color> onColorSelected;
        
        private static readonly Color[] PresetColors = new[]
        {
            new Color(0.2f, 0.8f, 0.2f),  // 绿色
            new Color(0.2f, 0.5f, 0.9f),  // 蓝色
            new Color(0.9f, 0.7f, 0.2f),  // 黄色
            new Color(0.9f, 0.3f, 0.3f),  // 红色
            new Color(0.7f, 0.3f, 0.9f),  // 紫色
            new Color(0.3f, 0.9f, 0.9f),  // 青色
            new Color(0.9f, 0.5f, 0.2f),  // 橙色
            new Color(0.9f, 0.3f, 0.7f),  // 粉色
            new Color(0.5f, 0.5f, 0.5f),  // 灰色
            new Color(0.9f, 0.9f, 0.9f),  // 白色
        };
        
        public override Vector2 InitialSize => new Vector2(350f, 300f);
        
        public Dialog_ColorPicker(Color initialColor, System.Action<Color> onColorSelected)
        {
            this.selectedColor = initialColor;
            this.onColorSelected = onColorSelected;
            
            doCloseButton = true;
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            
            Widgets.Label(listing.GetRect(22f), "RTE_ColorPicker_Title".Translate());
            listing.Gap();
            
            // 绘制预设颜色网格
            float colorSize = 40f;
            float spacing = 5f;
            int columns = 5;
            
            for (int i = 0; i < PresetColors.Length; i++)
            {
                int row = i / columns;
                int col = i % columns;
                
                Rect colorRect = new Rect(
                    inRect.x + col * (colorSize + spacing),
                    inRect.y + 40f + row * (colorSize + spacing),
                    colorSize,
                    colorSize
                );
                
                Widgets.DrawBoxSolid(colorRect, PresetColors[i]);
                
                if (Widgets.ButtonInvisible(colorRect))
                {
                    selectedColor = PresetColors[i];
                    onColorSelected?.Invoke(selectedColor);
                    Close();
                }
                
                // 高亮选中的颜色
                if (ColorDistance(selectedColor, PresetColors[i]) < 0.1f)
                {
                    Widgets.DrawBox(colorRect, 2);
                }
            }
            
            listing.End();
        }
        
        private float ColorDistance(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);
        }
    }
}
