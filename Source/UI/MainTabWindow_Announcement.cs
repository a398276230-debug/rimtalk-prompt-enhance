using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public class MainTabWindow_Announcement : MainTabWindow
    {
        private Vector2 taskScrollPosition;
        private string editingOverview;
        private bool isEditingOverview = false;
        
        // Tab state
        private AnnouncementCategory? selectedCategory = null; // null means "All"
        
        // Height cache
        private Dictionary<string, float> heightCache = new Dictionary<string, float>();
        private int lastDataVersion = -1;

        public override Vector2 InitialSize => new Vector2(1000f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            var manager = Current.Game.GetComponent<ColonyAnnouncementManager>();
            if (manager == null) return;

            // 上下布局
            // 上部：概况 (固定高度 180px)
            Rect topRect = inRect.TopPartPixels(180f);
            Widgets.DrawMenuSection(topRect);
            DrawOverviewSection(topRect.ContractedBy(10f), manager);

            // 下部：列表 (剩余空间)
            Rect bottomRect = new Rect(inRect.x, topRect.yMax + 10f, inRect.width, inRect.height - topRect.height - 10f);
            Widgets.DrawMenuSection(bottomRect);
            DrawAnnouncementSection(bottomRect.ContractedBy(10f), manager);
        }
        
        private void DrawOverviewSection(Rect rect, ColonyAnnouncementManager manager)
        {
            // 标题
            Rect titleRect = rect.TopPartPixels(24f);
            Text.Font = GameFont.Medium;
            Widgets.Label(titleRect, "📝 殖民地概况");
            Text.Font = GameFont.Small;
            
            // 文本区域
            if (!isEditingOverview)
            {
                editingOverview = manager.Data.ColonyOverview;
                isEditingOverview = true;
            }
            
            Rect textAreaRect = new Rect(rect.x, titleRect.yMax + 5f, rect.width, rect.height - 60f);
            string newText = Widgets.TextArea(textAreaRect, editingOverview);
            if (newText != editingOverview)
            {
                editingOverview = newText;
            }
            
            // 底部按钮和提示
            Rect bottomRow = new Rect(rect.x, textAreaRect.yMax + 5f, rect.width, 24f);
            
            if (Widgets.ButtonText(bottomRow.LeftPartPixels(120f), "保存概况"))
            {
                manager.Data.ColonyOverview = editingOverview;
                Messages.Message("殖民地概况已更新", MessageTypeDefOf.TaskCompletion, false);
            }
            
            Rect tipRect = new Rect(bottomRow.x + 130f, bottomRow.y, bottomRow.width - 130f, bottomRow.height);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(tipRect, "提示：用自然语言描述殖民地状态，AI会读取这些信息。");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
        
        private void DrawAnnouncementSection(Rect rect, ColonyAnnouncementManager manager)
        {
            // 1. 顶部工具栏：Tab + 新建按钮
            Rect toolbarRect = rect.TopPartPixels(30f);
            
            // 新建按钮 (右侧)
            Rect buttonRect = toolbarRect.RightPartPixels(120f);
            if (Widgets.ButtonText(buttonRect, "+ 新建状况"))
            {
                Find.WindowStack.Add(new TaskEditorDialog(null, manager));
            }
            
            // Tab 区域 (剩余左侧)
            Rect tabRect = new Rect(toolbarRect.x, toolbarRect.y, toolbarRect.width - 130f, 30f);
            DrawCategoryTabs(tabRect);
            
            // 2. 列表区域
            Rect listRect = new Rect(rect.x, toolbarRect.yMax + 10f, rect.width, rect.height - 40f);
            
            // 过滤列表
            var filteredList = manager.Data.Announcements
                .Where(a => selectedCategory == null || a.Category == selectedCategory)
                .OrderByDescending(a => a.Priority)
                .ToList();
            
            // 检查数据版本，如果变化则清空缓存
            if (manager.DataVersion != lastDataVersion)
            {
                heightCache.Clear();
                lastDataVersion = manager.DataVersion;
            }

            // 计算总高度和每个项的高度
            float totalHeight = 0f;
            float gap = 5f;
            float listWidth = listRect.width - 16f; // 减去滚动条宽度

            // 预计算高度（如果未缓存）
            foreach (var item in filteredList)
            {
                if (!heightCache.ContainsKey(item.Id))
                {
                    heightCache[item.Id] = CalculateItemHeight(item, listWidth);
                }
                totalHeight += heightCache[item.Id] + gap;
            }

            Rect viewRect = new Rect(0, 0, listWidth, totalHeight);
            Widgets.BeginScrollView(listRect, ref taskScrollPosition, viewRect);
            
            // 虚拟化渲染
            float scrollY = taskScrollPosition.y;
            float viewHeight = listRect.height;
            float currentY = 0f;

            foreach (var item in filteredList)
            {
                float h = heightCache[item.Id];
                
                // 只绘制可见区域内的项
                if (currentY + h >= scrollY && currentY <= scrollY + viewHeight)
                {
                    DrawAnnouncementItem(new Rect(0, currentY, listWidth, h), item, manager);
                }
                
                currentY += h + gap;
                
                // 如果已经超出可视区域，可以提前退出循环（优化）
                if (currentY > scrollY + viewHeight) break;
            }
            
            Widgets.EndScrollView();
        }

        private float CalculateItemHeight(ColonyAnnouncement item, float width)
        {
            float baseHeight = 34f; // 顶部标题行 + 间距
            float rightWidth = 160f;
            float textWidth = width - rightWidth - 20f; // 减去左侧颜色条和右侧按钮
            
            // 计算描述文本高度
            string desc = item.Description;
            string extra = "";
            if (item.Category == AnnouncementCategory.Project && item.Progress > 0)
                extra += $" [进度: {item.Progress:P0}]";
            if (!string.IsNullOrEmpty(item.AssignedPawnName))
                extra += $" [负责人: {item.AssignedPawnName}]";
            
            string fullText = desc + extra;
            
            float textHeight = Text.CalcHeight(fullText, textWidth);
            
            return Mathf.Max(60f, baseHeight + textHeight + 10f);
        }
        
        private void DrawCategoryTabs(Rect rect)
        {
            List<TabRecord> tabs = new List<TabRecord>();
            
            // "全部" 标签
            tabs.Add(new TabRecord("全部", () => selectedCategory = null, selectedCategory == null));
            
            // 各类别标签
            foreach (AnnouncementCategory cat in Enum.GetValues(typeof(AnnouncementCategory)))
            {
                tabs.Add(new TabRecord(GetCategoryLabel(cat), () => selectedCategory = cat, selectedCategory == cat));
            }
            
            TabDrawer.DrawTabs(rect, tabs);
        }
        
        private string GetCategoryLabel(AnnouncementCategory cat)
        {
            return cat switch
            {
                AnnouncementCategory.Project => "工程",
                AnnouncementCategory.Event => "事件",
                AnnouncementCategory.Quest => "任务",
                AnnouncementCategory.Resource => "资源",
                AnnouncementCategory.Personnel => "人员",
                AnnouncementCategory.Custom => "自定义",
                _ => cat.ToString()
            };
        }
        
        private void DrawAnnouncementItem(Rect rect, ColonyAnnouncement item, ColonyAnnouncementManager manager)
        {
            // 绘制卡片背景
            Widgets.DrawOptionBackground(rect, false);
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            
            // 优先级颜色条
            Color priorityColor = item.Priority switch
            {
                AnnouncementPriority.Urgent => new Color(1f, 0.3f, 0.3f),
                AnnouncementPriority.High => new Color(1f, 0.7f, 0.2f),
                AnnouncementPriority.Normal => new Color(0.4f, 0.8f, 0.4f),
                _ => new Color(0.7f, 0.7f, 0.7f)
            };
            Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, 4f, rect.height), priorityColor);
            
            Rect contentRect = rect.ContractedBy(6f);
            contentRect.xMin += 6f;
            
            // 布局：左侧信息，右侧按钮
            float rightWidth = 160f;
            Rect infoRect = new Rect(contentRect.x, contentRect.y, contentRect.width - rightWidth, contentRect.height);
            Rect btnRect = new Rect(contentRect.xMax - rightWidth, contentRect.y, rightWidth, contentRect.height);
            
            // === 信息区域 ===
            // 第一行：[类别] 标题
            Rect line1 = infoRect.TopPartPixels(24f);
            
            // 类别标签
            string catLabel = GetCategoryLabel(item.Category);
            Vector2 catSize = Text.CalcSize(catLabel);
            Rect catRect = new Rect(line1.x, line1.y + 2f, catSize.x + 8f, 20f);
            Widgets.DrawBoxSolid(catRect, new Color(0.25f, 0.25f, 0.25f));
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(catRect, catLabel);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 标题
            Rect titleRect = new Rect(catRect.xMax + 8f, line1.y, line1.width - catRect.width - 8f, 24f);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            if (item.Status == AnnouncementStatus.Completed) GUI.color = Color.gray;
            Widgets.Label(titleRect, item.Title);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 第二行：描述 + 额外信息 (动态高度)
            Rect line2 = new Rect(infoRect.x, line1.yMax, infoRect.width, infoRect.height - line1.height);
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            Text.Anchor = TextAnchor.UpperLeft; // 改为左上对齐，支持多行
            
            string desc = item.Description;
            // 不再截断文本
            
            string extra = "";
            if (item.Category == AnnouncementCategory.Project && item.Progress > 0)
                extra += $" [进度: {item.Progress:P0}]";
            if (!string.IsNullOrEmpty(item.AssignedPawnName))
                extra += $" [负责人: {item.AssignedPawnName}]";
                
            Widgets.Label(line2, $"{desc} {extra}");
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            // === 按钮区域 ===
            // 垂直居中
            float btnY = btnRect.y + (btnRect.height - 24f) / 2f;
            float btnW = 50f;
            float gap = 4f;
            
            // 状态按钮
            Rect statusBtn = new Rect(btnRect.x, btnY, btnW, 24f);
            string statusLabel = item.Status == AnnouncementStatus.Active ? "完成" : 
                                 item.Status == AnnouncementStatus.Paused ? "恢复" : "重开";
            if (Widgets.ButtonText(statusBtn, statusLabel))
            {
                if (item.Status == AnnouncementStatus.Active) { item.Status = AnnouncementStatus.Completed; item.CompletedTick = Find.TickManager.TicksGame; }
                else if (item.Status == AnnouncementStatus.Paused) item.Status = AnnouncementStatus.Active;
                else item.Status = AnnouncementStatus.Active;
                manager.NotifyDataChanged();
            }
            
            // 编辑按钮
            Rect editBtn = new Rect(statusBtn.xMax + gap, btnY, btnW, 24f);
            if (Widgets.ButtonText(editBtn, "编辑"))
            {
                Find.WindowStack.Add(new TaskEditorDialog(item, manager));
            }
            
            // 删除按钮
            Rect delBtn = new Rect(editBtn.xMax + gap, btnY, btnW, 24f);
            if (Widgets.ButtonText(delBtn, "删除"))
            {
                manager.DeleteAnnouncement(item.Id);
            }
        }
    }
}
