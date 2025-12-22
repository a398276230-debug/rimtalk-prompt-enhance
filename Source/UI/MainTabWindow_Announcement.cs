using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public class MainTabWindow_Announcement : MainTabWindow
    {
        private enum Tab { Current, History }
        private Tab currentTab = Tab.Current;
        
        private Vector2 taskScrollPosition;
        private Vector2 snapshotScrollPos;
        private string editingOverview;
        private bool isEditingOverview = false;
        
        // Tab state
        private AnnouncementCategory? selectedCategory = null; // null means "All"
        private int currentSnapshotIndex = 0;
        
        // Height cache
        private Dictionary<string, float> heightCache = new Dictionary<string, float>();
        private int lastDataVersion = -1;

        public override Vector2 InitialSize => new Vector2(1000f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            var manager = Current.Game.GetComponent<ColonyAnnouncementManager>();
            if (manager == null) return;

            // 顶部标签页 (注意：TabDrawer 画在 rect 上方，所以 y 需要留出空间)
            Rect tabRect = new Rect(inRect.x, inRect.y + 32f, inRect.width, 0f);
            DrawMainTabs(tabRect);
            
            // 内容区域
            Rect contentRect = new Rect(inRect.x, inRect.y + 35f, inRect.width, inRect.height - 35f);
            
            switch (currentTab)
            {
                case Tab.Current:
                    DrawCurrentTab(contentRect, manager);
                    break;
                case Tab.History:
                    DrawSnapshotsTab(contentRect, manager);
                    break;
            }
        }
        
        private void DrawMainTabs(Rect rect)
        {
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("当前状态", () => currentTab = Tab.Current, currentTab == Tab.Current),
                new TabRecord("历史快照", () => currentTab = Tab.History, currentTab == Tab.History)
            };
            
            TabDrawer.DrawTabs(rect, tabs);
        }
        
        private void DrawCurrentTab(Rect rect, ColonyAnnouncementManager manager)
        {
            // 上下布局：上部概况，下部列表
            
            // 上部：概况 (固定高度 180px)
            Rect topRect = rect.TopPartPixels(180f);
            Widgets.DrawMenuSection(topRect);
            DrawOverviewSection(topRect.ContractedBy(10f), manager);

            // 下部：列表 (剩余空间)
            Rect bottomRect = new Rect(rect.x, topRect.yMax + 10f, rect.width, rect.height - topRect.height - 10f);
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

            // AI 总结按钮
            if (Widgets.ButtonText(new Rect(bottomRow.x + 130f, bottomRow.y, 120f, bottomRow.height), "AI 总结概况"))
            {
                if (string.IsNullOrWhiteSpace(editingOverview))
                {
                    Messages.Message("概况为空，无法总结", MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "确定要让 AI 总结并精简当前的概况吗？\n这将替换当前的文本。",
                        () => 
                        {
                            System.Threading.Tasks.Task.Run(async () => 
                            {
                                var settings = RimTalkHealthEnhanceMod.Settings;
                                string prompt;
                                
                                // 使用自定义提示词或默认提示词
                                if (!string.IsNullOrEmpty(settings.CustomOverviewSummaryPrompt))
                                {
                                    prompt = settings.CustomOverviewSummaryPrompt
                                        .Replace("{overview}", editingOverview);
                                }
                                else
                                {
                                    prompt = $@"请将以下RimWorld殖民地概况进行总结和精简。

【原文】
{editingOverview}

【要求】
1. **严格保持原文的语言风格**：如果原文是第一人称（我/我们），必须保持；如果是第三人称，必须保持。如果原文幽默，保持幽默；如果严肃，保持严肃。
2. 保留关键信息（殖民地名称、重要事件、核心设施、主要人物等）。
3. 去除冗余和重复的描述。
4. 控制在原文的60-80%长度。
5. 不要添加原文中没有的内容或改变叙事视角。
6. 直接输出精简后的文本，不要写""总结如下""之类的开头。";
                                }

                                string result = await SimpleAIClient.CallAI(prompt);
                                if (!string.IsNullOrEmpty(result))
                                {
                                    editingOverview = result;
                                    // 自动保存
                                    manager.Data.ColonyOverview = result;
                                    manager.NotifyDataChanged();
                                    Messages.Message("概况已由 AI 总结更新", MessageTypeDefOf.PositiveEvent, false);
                                }
                                else
                                {
                                    Messages.Message("AI 总结失败", MessageTypeDefOf.NegativeEvent, false);
                                }
                            });
                        }
                    ));
                }
            }
            
            // 自定义提示词按钮
            if (Widgets.ButtonText(new Rect(bottomRow.x + 260f, bottomRow.y, 100f, bottomRow.height), "⚙ 提示词"))
            {
                string defaultPrompt = @"请将以下RimWorld殖民地概况进行总结和精简。

【原文】
{overview}

【要求】
1. **严格保持原文的语言风格**：如果原文是第一人称（我/我们），必须保持；如果是第三人称，必须保持。如果原文幽默，保持幽默；如果严肃，保持严肃。
2. 保留关键信息（殖民地名称、重要事件、核心设施、主要人物等）。
3. 去除冗余和重复的描述。
4. 控制在原文的60-80%长度。
5. 不要添加原文中没有的内容或改变叙事视角。
6. 直接输出精简后的文本，不要写""总结如下""之类的开头。";

                Find.WindowStack.Add(new PromptEditorDialog(
                    "自定义概况总结提示词",
                    RimTalkHealthEnhanceMod.Settings.CustomOverviewSummaryPrompt,
                    defaultPrompt,
                    (newPrompt) => 
                    {
                        RimTalkHealthEnhanceMod.Settings.CustomOverviewSummaryPrompt = newPrompt;
                        RimTalkHealthEnhanceMod.Settings.Write();
                    }
                ));
            }
            
            Rect tipRect = new Rect(bottomRow.x + 370f, bottomRow.y, bottomRow.width - 370f, bottomRow.height);
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(tipRect, "提示：用自然语言描述殖民地状态，AI会读取这些信息。");
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
        
        private void DrawSnapshotsTab(Rect rect, ColonyAnnouncementManager manager)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);
            
            var snapshots = manager.Data.DailySnapshots
                .OrderByDescending(s => s.Day)
                .ToList();
            
            if (!snapshots.Any())
            {
                Widgets.Label(innerRect, "暂无快照记录。每日 0 点将自动生成快照。");
                return;
            }
            
            // 确保索引有效
            currentSnapshotIndex = Mathf.Clamp(currentSnapshotIndex, 0, snapshots.Count - 1);
            var snapshot = snapshots[currentSnapshotIndex];
            
            // 顶部：日期导航
            var navRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            DrawSnapshotNavigation(navRect, snapshot, snapshots.Count);
            
            // 内容区域
            var contentRect = new Rect(innerRect.x, innerRect.y + 35f, innerRect.width, innerRect.height - 70f);
            var scrollRect = new Rect(0, 0, contentRect.width - 20f, 1500f); // 估算高度，或者动态计算
            
            Widgets.BeginScrollView(contentRect, ref snapshotScrollPos, scrollRect);
            
            float curY = 0f;
            float width = scrollRect.width;
            
            // AI 总结区域
            if (!string.IsNullOrEmpty(snapshot.AISummary))
            {
                Widgets.Label(new Rect(0, curY, width, 25f), "【AI 总结】");
                curY += 30f;
                
                var summaryHeight = Text.CalcHeight(snapshot.AISummary, width - 20f);
                Widgets.DrawBoxSolid(new Rect(0, curY, width, summaryHeight + 20f), new Color(0.2f, 0.3f, 0.4f, 0.3f));
                Widgets.Label(new Rect(10f, curY + 10f, width - 20f, summaryHeight), snapshot.AISummary);
                curY += summaryHeight + 30f;
            }
            
            // 详细变化
            Widgets.Label(new Rect(0, curY, width, 25f), "📊 详细变化");
            curY += 30f;
            
            var diffHeight = Text.CalcHeight(snapshot.DiffReport, width - 20f);
            Widgets.DrawBoxSolid(new Rect(0, curY, width, diffHeight + 20f), new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Widgets.Label(new Rect(10f, curY + 10f, width - 20f, diffHeight), snapshot.DiffReport);
            curY += diffHeight + 30f;
            
            // 玩家操作
            if (snapshot.PlayerActions.Any())
            {
                Widgets.Label(new Rect(0, curY, width, 25f), "【玩家操作】");
                curY += 30f;
                foreach (var action in snapshot.PlayerActions)
                {
                    Widgets.Label(new Rect(10f, curY, width - 20f, 25f), action);
                    curY += 25f;
                }
                curY += 10f;
            }
            
            // 事件记录
            if (snapshot.Events.Any())
            {
                Widgets.Label(new Rect(0, curY, width, 25f), "【事件记录】");
                curY += 30f;
                foreach (var evt in snapshot.Events)
                {
                    Widgets.Label(new Rect(10f, curY, width - 20f, 25f), $"- {evt}");
                    curY += 25f;
                }
            }
            
            Widgets.EndScrollView();
            
            // 底部按钮
            var buttonRect = new Rect(innerRect.x, innerRect.yMax - 30f, innerRect.width, 30f);
            DrawSnapshotButtons(buttonRect, snapshot);
        }
        
        private void DrawSnapshotNavigation(Rect rect, DailySnapshot snapshot, int totalCount)
        {
            // 左箭头
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, 80f, rect.height), "← 前一天"))
            {
                currentSnapshotIndex = Mathf.Min(currentSnapshotIndex + 1, totalCount - 1);
            }
            
            // 日期显示 - 使用游戏实际日期
            string dateStr = GenDate.DateFullStringAt(snapshot.Tick, Vector2.zero);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(rect.x + 90f, rect.y, rect.width - 180f, rect.height), dateStr);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 右箭头
            if (Widgets.ButtonText(new Rect(rect.xMax - 80f, rect.y, 80f, rect.height), "后一天 →"))
            {
                currentSnapshotIndex = Mathf.Max(currentSnapshotIndex - 1, 0);
            }
        }
        
        private void DrawSnapshotButtons(Rect rect, DailySnapshot snapshot)
        {
            float buttonWidth = (rect.width - 20f) / 3f;
            
            // 复制到概况
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, buttonWidth, rect.height), "复制到概况"))
            {
                var manager = ColonyAnnouncementManager.Instance;
                
                // 如果有未保存的编辑内容，先保存
                if (isEditingOverview)
                {
                    manager.Data.ColonyOverview = editingOverview;
                }
                
                // 使用游戏实际日期
                string dateHeader = $"[{GenDate.DateFullStringAt(snapshot.Tick, Vector2.zero)}]";
                
                // 追加新内容（带日期）
                manager.Data.ColonyOverview += $"\n\n{dateHeader}\n{snapshot.AISummary}";
                
                // 重置编辑状态，强制下次进入概况页时重新加载最新数据
                isEditingOverview = false;
                
                manager.NotifyDataChanged();
                Messages.Message("已追加到概况（含日期）", MessageTypeDefOf.TaskCompletion, false);
            }
            
            // 重新生成
            if (Widgets.ButtonText(new Rect(rect.x + buttonWidth + 10f, rect.y, buttonWidth, rect.height), "重新生成"))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "确定要重新生成此快照的 AI 总结吗？",
                    () => 
                    {
                        // 使用 Task.Run 避免阻塞
                        System.Threading.Tasks.Task.Run(async () => 
                        {
                            string prompt = MidnightSynthesisService.BuildSynthesisPrompt(snapshot.DiffReport, snapshot);
                            string result = await SimpleAIClient.CallAI(prompt);
                            if (!string.IsNullOrEmpty(result))
                            {
                                snapshot.AISummary = result;
                                ColonyAnnouncementManager.Instance.NotifyDataChanged();
                                Messages.Message("AI 总结已更新", MessageTypeDefOf.PositiveEvent, false);
                            }
                            else
                            {
                                Messages.Message("AI 总结生成失败", MessageTypeDefOf.NegativeEvent, false);
                            }
                        });
                    }
                ));
            }
            
            // 自定义提示词按钮
            if (Widgets.ButtonText(new Rect(rect.x + buttonWidth * 2 + 20f, rect.y, buttonWidth, rect.height), "⚙ 提示词"))
            {
                string defaultPrompt = MidnightSynthesisService.GetDefaultPromptTemplate();
                
                Find.WindowStack.Add(new PromptEditorDialog(
                    "自定义每日快照提示词",
                    RimTalkHealthEnhanceMod.Settings.CustomDailySynthesisPrompt,
                    defaultPrompt,
                    (newPrompt) => 
                    {
                        RimTalkHealthEnhanceMod.Settings.CustomDailySynthesisPrompt = newPrompt;
                        RimTalkHealthEnhanceMod.Settings.Write();
                    }
                ));
            }
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
