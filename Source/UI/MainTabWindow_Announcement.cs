using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using RimTalkHealthEnhance.Services;

namespace RimTalkHealthEnhance
{
    public class MainTabWindow_Announcement : MainTabWindow
    {
        private enum Tab { Current, History, CustomAreas }
        private Tab currentTab = Tab.Current;
        
        private Vector2 taskScrollPosition;
        private Vector2 snapshotScrollPos;
        private Vector2 areaScrollPos;
        private string editingOverview;
        private bool isEditingOverview = false;
        
        // Tab state
        private AnnouncementCategory? selectedCategory = null; // null means "All"
        private int currentSnapshotIndex = 0;
        
        // Height cache
        private Dictionary<string, float> heightCache = new Dictionary<string, float>();
        private int lastDataVersion = -1;
        
        // AI Summary editing state
        private string editingSummary;
        private long editingSnapshotAbsTick = -1;  // 使用 AbsTick 作为标识

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
                case Tab.CustomAreas:
                    DrawCustomAreasTab(contentRect, manager);
                    break;
            }
        }
        
        private void DrawMainTabs(Rect rect)
        {
            List<TabRecord> tabs = new List<TabRecord>
            {
                new TabRecord("RTE_Tab_CurrentStatus".Translate(), () => currentTab = Tab.Current, currentTab == Tab.Current),
                new TabRecord("RTE_Tab_HistorySnapshots".Translate(), () => currentTab = Tab.History, currentTab == Tab.History),
                new TabRecord("RTE_Tab_CustomAreas".Translate(), () => currentTab = Tab.CustomAreas, currentTab == Tab.CustomAreas)
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
            Widgets.Label(titleRect, "RTE_ColonyOverview_Title".Translate());
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
            
            if (Widgets.ButtonText(bottomRow.LeftPartPixels(120f), "RTE_ColonyOverview_Save".Translate()))
            {
                manager.Data.ColonyOverview = editingOverview;
                Messages.Message("RTE_ColonyOverview_Updated".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }

            // AI 总结按钮
            if (Widgets.ButtonText(new Rect(bottomRow.x + 130f, bottomRow.y, 120f, bottomRow.height), "RTE_ColonyOverview_AISummary".Translate()))
            {
                if (string.IsNullOrWhiteSpace(editingOverview))
                {
                    Messages.Message("RTE_ColonyOverview_EmptyError".Translate(), MessageTypeDefOf.RejectInput, false);
                }
                else
                {
                    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                        "RTE_ColonyOverview_ConfirmSummary".Translate(),
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
                                    Messages.Message("RTE_ColonyOverview_SummarySuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
                                }
                                else
                                {
                                    Messages.Message("RTE_ColonyOverview_SummaryFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                                }
                            });
                        }
                    ));
                }
            }
            
            // 自定义提示词按钮
            if (Widgets.ButtonText(new Rect(bottomRow.x + 260f, bottomRow.y, 100f, bottomRow.height), "RTE_ColonyOverview_CustomPrompt".Translate()))
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
                    "RTE_PromptEditor_CustomPrompt".Translate("RTE_PromptEditor_OverviewSummary".Translate()),
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
            Widgets.Label(tipRect, "RTE_ColonyOverview_Tip".Translate());
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
        
        private void DrawSnapshotsTab(Rect rect, ColonyAnnouncementManager manager)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);
            
            var snapshots = manager.Data.DailySnapshots
                .OrderByDescending(s => s.AbsTick)
                .ToList();
            
            if (!snapshots.Any())
            {
                Widgets.Label(innerRect, "RTE_Snapshot_NoData".Translate());
                return;
            }
            
            // 确保索引有效
            currentSnapshotIndex = Mathf.Clamp(currentSnapshotIndex, 0, snapshots.Count - 1);
            var snapshot = snapshots[currentSnapshotIndex];
            
            // 顶部：日期导航
            var navRect = new Rect(innerRect.x, innerRect.y, innerRect.width, 30f);
            DrawSnapshotNavigation(navRect, snapshot, snapshots.Count, manager);
            
            // 内容区域（预留重置按钮的空间）
            var contentRect = new Rect(innerRect.x, innerRect.y + 65f, innerRect.width, innerRect.height - 100f);
            var scrollRect = new Rect(0, 0, contentRect.width - 20f, 1500f); // 估算高度，或者动态计算
            
            Widgets.BeginScrollView(contentRect, ref snapshotScrollPos, scrollRect);
            
            float curY = 0f;
            float width = scrollRect.width;
            
            // AI 总结区域 - 可编辑
            Widgets.Label(new Rect(0, curY, width - 100f, 25f), "RTE_Snapshot_AISummary".Translate());
            
            // 保存按钮 - 在标题右侧
            if (Widgets.ButtonText(new Rect(width - 90f, curY, 90f, 24f), "RTE_Snapshot_SaveSummary".Translate()))
            {
                if (editingSnapshotAbsTick == snapshot.AbsTick && editingSummary != null)
                {
                    snapshot.AISummary = editingSummary;
                    manager.NotifyDataChanged();
                    Messages.Message("RTE_Snapshot_SummarySaved".Translate(), MessageTypeDefOf.TaskCompletion, false);
                }
            }
            curY += 30f;
            
            // 初始化编辑状态
            if (editingSnapshotAbsTick != snapshot.AbsTick)
            {
                editingSummary = snapshot.AISummary ?? "";
                editingSnapshotAbsTick = snapshot.AbsTick;
            }
            
            // 可编辑文本框
            float summaryHeight = Mathf.Max(100f, Text.CalcHeight(editingSummary ?? "", width - 20f) + 20f);
            Widgets.DrawBoxSolid(new Rect(0, curY, width, summaryHeight + 10f), new Color(0.2f, 0.3f, 0.4f, 0.3f));
            string newSummary = Widgets.TextArea(new Rect(5f, curY + 5f, width - 10f, summaryHeight), editingSummary ?? "");
            if (newSummary != editingSummary)
            {
                editingSummary = newSummary;
            }
            curY += summaryHeight + 20f;
            
            // 详细变化
            Widgets.Label(new Rect(0, curY, width, 25f), "RTE_Snapshot_DetailedChanges".Translate());
            curY += 30f;
            
            var diffHeight = Text.CalcHeight(snapshot.DiffReport, width - 20f);
            Widgets.DrawBoxSolid(new Rect(0, curY, width, diffHeight + 20f), new Color(0.1f, 0.1f, 0.1f, 0.3f));
            Widgets.Label(new Rect(10f, curY + 10f, width - 20f, diffHeight), snapshot.DiffReport);
            curY += diffHeight + 30f;
            
            // 玩家操作
            if (snapshot.PlayerActions.Any())
            {
                Widgets.Label(new Rect(0, curY, width, 25f), "RTE_Snapshot_PlayerActions".Translate());
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
                Widgets.Label(new Rect(0, curY, width, 25f), "RTE_Snapshot_Events".Translate());
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
        
        private void DrawSnapshotNavigation(Rect rect, DailySnapshot snapshot, int totalCount, ColonyAnnouncementManager manager)
        {
            float btnWidth = 70f;
            float dateAdjustBtnWidth = 45f;
            float gap = 5f;
            
            // 左箭头 - 切换到更早的快照
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, btnWidth, rect.height), "RTE_Snapshot_PrevDay".Translate()))
            {
                currentSnapshotIndex = Mathf.Min(currentSnapshotIndex + 1, totalCount - 1);
                // 切换快照时重置编辑状态
                editingSnapshotAbsTick = -1;
            }
            
            // 日期后退按钮 (-1天) - 调整全局偏移量（只影响显示）
            float dateBackX = rect.x + btnWidth + gap;
            if (Widgets.ButtonText(new Rect(dateBackX, rect.y, dateAdjustBtnWidth, rect.height), "RTE_Snapshot_DateBack".Translate()))
            {
                manager.Data.DisplayTickOffset -= GenDate.TicksPerDay;
                manager.NotifyDataChanged();
                Messages.Message("RTE_Snapshot_DateAdjusted".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }
            
            // 日期显示 - 使用 DailySnapshot 的方法获取显示日期
            float dateDisplayX = dateBackX + dateAdjustBtnWidth + gap;
            float dateDisplayWidth = rect.width - (btnWidth * 2) - (dateAdjustBtnWidth * 2) - (gap * 4);
            string gameDateStr = snapshot.GetDateStringWithOffset(manager.Data.DisplayTickOffset, Vector2.zero);
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(dateDisplayX, rect.y, dateDisplayWidth, rect.height), gameDateStr);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 日期前进按钮 (+1天) - 调整全局偏移量（只影响显示）
            float dateForwardX = dateDisplayX + dateDisplayWidth + gap;
            if (Widgets.ButtonText(new Rect(dateForwardX, rect.y, dateAdjustBtnWidth, rect.height), "RTE_Snapshot_DateForward".Translate()))
            {
                manager.Data.DisplayTickOffset += GenDate.TicksPerDay;
                manager.NotifyDataChanged();
                Messages.Message("RTE_Snapshot_DateAdjusted".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }
            
            // 右箭头 - 切换到更近的快照
            if (Widgets.ButtonText(new Rect(rect.xMax - btnWidth, rect.y, btnWidth, rect.height), "RTE_Snapshot_NextDay".Translate()))
            {
                currentSnapshotIndex = Mathf.Max(currentSnapshotIndex - 1, 0);
                // 切换快照时重置编辑状态
                editingSnapshotAbsTick = -1;
            }
            
            // 重置日期偏移按钮 - 单独一行居中（仅当偏移量不为0时显示，也检查旧版本字段）
            #pragma warning disable CS0612
            bool hasOffset = manager.Data.DisplayTickOffset != 0 ||
                             manager.Data.SnapshotDayOffset != 0 ||
                             manager.Data.SnapshotTickOffset != 0;
            #pragma warning restore CS0612
            
            if (hasOffset)
            {
                var resetRect = new Rect(rect.x, rect.y + rect.height + 5f, rect.width, 24f);
                float resetBtnWidth = 100f;
                Rect resetButtonRect = new Rect(resetRect.x + (resetRect.width - resetBtnWidth) / 2f, resetRect.y, resetBtnWidth, resetRect.height);
                if (Widgets.ButtonText(resetButtonRect, "RTE_Snapshot_ResetOffset".Translate()))
                {
                    // 重置新版本字段
                    manager.Data.DisplayTickOffset = 0;
                    
                    // 同时重置旧版本字段，防止下次加载时再次迁移
                    #pragma warning disable CS0612
                    manager.Data.SnapshotDayOffset = 0;
                    manager.Data.SnapshotTickOffset = 0;
                    #pragma warning restore CS0612
                    
                    manager.NotifyDataChanged();
                    Messages.Message("RTE_Snapshot_OffsetReset".Translate(), MessageTypeDefOf.TaskCompletion, false);
                }
            }
        }
        
        private void DrawSnapshotButtons(Rect rect, DailySnapshot snapshot)
        {
            float buttonWidth = (rect.width - 20f) / 3f;
            
            // 复制到概况
            if (Widgets.ButtonText(new Rect(rect.x, rect.y, buttonWidth, rect.height), "RTE_Snapshot_CopyToOverview".Translate()))
            {
                var manager = ColonyAnnouncementManager.Instance;
                
                // 如果有未保存的编辑内容，先保存
                if (isEditingOverview)
                {
                    manager.Data.ColonyOverview = editingOverview;
                }
                
                // 使用 DailySnapshot 的方法获取显示日期（带偏移量）
                string dateHeader = $"[{snapshot.GetDateStringWithOffset(manager.Data.DisplayTickOffset, Vector2.zero)}]";
                
                // 追加新内容（带日期）
                manager.Data.ColonyOverview += $"\n\n{dateHeader}\n{snapshot.AISummary}";
                
                // 重置编辑状态，强制下次进入概况页时重新加载最新数据
                isEditingOverview = false;
                
                manager.NotifyDataChanged();
                Messages.Message("RTE_Snapshot_CopiedWithDate".Translate(), MessageTypeDefOf.TaskCompletion, false);
            }
            
            // 重新生成
            if (Widgets.ButtonText(new Rect(rect.x + buttonWidth + 10f, rect.y, buttonWidth, rect.height), "RTE_Snapshot_Regenerate".Translate()))
            {
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RTE_Snapshot_ConfirmRegenerate".Translate(),
                    () => 
                    {
                        // 使用 Task.Run 避免阻塞
                        System.Threading.Tasks.Task.Run(async () => 
                        {
                            var settings = RimTalkHealthEnhanceMod.Settings;
                            var manager = ColonyAnnouncementManager.Instance;
                            
                            // 重新收集工程和科技信息
                            List<string> projectInfo = new List<string>();
                            if (settings.IncludeProjectsInSnapshot)
                            {
                                var activeProjects = manager.Data.Announcements
                                    .Where(a => a.Category == AnnouncementCategory.Project)
                                    .ToList();
                                
                                foreach (var project in activeProjects)
                                {
                                    string statusText = project.Status == AnnouncementStatus.Completed ? "RTE_Announcement_Status_Completed".Translate() : 
                                                       project.Status == AnnouncementStatus.Paused ? "RTE_Announcement_Status_Paused".Translate() : "RTE_Announcement_Status_Active".Translate();
                                    string progressText = project.Progress > 0 ? $" ({project.Progress:P0})" : "";
                                    string assignedText = !string.IsNullOrEmpty(project.AssignedPawnName) ? $" - 负责人: {project.AssignedPawnName}" : "";
                                    
                                    projectInfo.Add($"{statusText} {project.Title}{progressText}{assignedText}");
                                }
                            }
                            
                            string researchInfo = null;
                            if (settings.IncludeResearchInSnapshot)
                            {
                                researchInfo = ResearchInfoBuilder.BuildResearchContext();
                            }
                            
                            string prompt = MidnightSynthesisService.BuildSynthesisPrompt(snapshot.DiffReport, snapshot, projectInfo, researchInfo);
                            string result = await SimpleAIClient.CallAI(prompt);
                            if (!string.IsNullOrEmpty(result))
                            {
                                snapshot.AISummary = result;
                                // 重置编辑状态，强制编辑框重新加载新内容
                                editingSnapshotAbsTick = -1;
                                editingSummary = null;
                                manager.NotifyDataChanged();
                                Messages.Message("RTE_Snapshot_RegenerateSuccess".Translate(), MessageTypeDefOf.PositiveEvent, false);
                            }
                            else
                            {
                                Messages.Message("RTE_Snapshot_RegenerateFailed".Translate(), MessageTypeDefOf.NegativeEvent, false);
                            }
                        });
                    }
                ));
            }
            
            // 自定义提示词按钮
            if (Widgets.ButtonText(new Rect(rect.x + buttonWidth * 2 + 20f, rect.y, buttonWidth, rect.height), "RTE_ColonyOverview_CustomPrompt".Translate()))
            {
                string defaultPrompt = MidnightSynthesisService.GetDefaultPromptTemplate();
                
                Find.WindowStack.Add(new PromptEditorDialog(
                    "RTE_PromptEditor_CustomPrompt".Translate("RTE_PromptEditor_DailySynthesis".Translate()),
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
            if (Widgets.ButtonText(buttonRect, "RTE_Announcement_NewTask".Translate()))
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
            float rightWidth = 210f; // 增加宽度以容纳讨论按钮
            float textWidth = width - rightWidth - 20f; // 减去左侧颜色条和右侧按钮
            
            // 计算描述文本高度
            string desc = item.Description;
            string extra = "";
            if (item.Category == AnnouncementCategory.Project && item.Progress > 0)
                extra += "RTE_Announcement_Progress_Display".Translate(item.Progress.ToStringPercent());
            if (!string.IsNullOrEmpty(item.AssignedPawnName))
                extra += "RTE_Announcement_AssignedTo_Display".Translate(item.AssignedPawnName);
            
            string fullText = desc + extra;
            
            float textHeight = Text.CalcHeight(fullText, textWidth);
            
            return Mathf.Max(60f, baseHeight + textHeight + 10f);
        }
        
        private void DrawCategoryTabs(Rect rect)
        {
            List<TabRecord> tabs = new List<TabRecord>();
            
            // "RTE_Announcement_AllCategories".Translate() 标签
            tabs.Add(new TabRecord("RTE_Announcement_AllCategories".Translate(), () => selectedCategory = null, selectedCategory == null));
            
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
                AnnouncementCategory.Project => "RTE_Announcement_Category_Project".Translate(),
                AnnouncementCategory.Event => "RTE_Announcement_Category_Event".Translate(),
                AnnouncementCategory.Quest => "RTE_Announcement_Category_Quest".Translate(),
                AnnouncementCategory.Resource => "RTE_Announcement_Category_Resource".Translate(),
                AnnouncementCategory.Personnel => "RTE_Announcement_Category_Personnel".Translate(),
                AnnouncementCategory.Custom => "RTE_Announcement_Category_Custom".Translate(),
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
            float rightWidth = 210f; // 增加宽度以容纳讨论按钮
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
                extra += "RTE_Announcement_Progress_Display".Translate(item.Progress.ToStringPercent());
            if (!string.IsNullOrEmpty(item.AssignedPawnName))
                extra += "RTE_Announcement_AssignedTo_Display".Translate(item.AssignedPawnName);
                
            Widgets.Label(line2, $"{desc} {extra}");
            
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            // === 按钮区域 ===
            // 垂直居中
            float btnY = btnRect.y + (btnRect.height - 24f) / 2f;
            float btnW = 50f;
            float gap = 4f;
            
            // 讨论按钮 - 最左边
            Rect discussBtn = new Rect(btnRect.x, btnY, btnW, 24f);
            if (Widgets.ButtonText(discussBtn, "RTE_Announcement_Discuss".Translate()))
            {
                ShowPawnSelectorMenu(item);
            }
            
            // 状态按钮
            Rect statusBtn = new Rect(discussBtn.xMax + gap, btnY, btnW, 24f);
            string statusLabel = item.Status == AnnouncementStatus.Active ? "RTE_Announcement_Complete".Translate() :
                                 item.Status == AnnouncementStatus.Paused ? "RTE_Announcement_Resume".Translate() : "RTE_Announcement_Reopen".Translate();
            if (Widgets.ButtonText(statusBtn, statusLabel))
            {
                if (item.Status == AnnouncementStatus.Active) { item.Status = AnnouncementStatus.Completed; item.CompletedTick = Find.TickManager.TicksGame; }
                else if (item.Status == AnnouncementStatus.Paused) item.Status = AnnouncementStatus.Active;
                else item.Status = AnnouncementStatus.Active;
                manager.NotifyDataChanged();
            }
            
            // 编辑按钮
            Rect editBtn = new Rect(statusBtn.xMax + gap, btnY, btnW, 24f);
            if (Widgets.ButtonText(editBtn, "RTE_Announcement_Edit".Translate()))
            {
                Find.WindowStack.Add(new TaskEditorDialog(item, manager));
            }
            
            // 删除按钮
            Rect delBtn = new Rect(editBtn.xMax + gap, btnY, btnW, 24f);
            if (Widgets.ButtonText(delBtn, "RTE_Announcement_Delete".Translate()))
            {
                manager.DeleteAnnouncement(item.Id);
            }
        }
        
        private void DrawCustomAreasTab(Rect rect, ColonyAnnouncementManager manager)
        {
            Widgets.DrawMenuSection(rect);
            Rect innerRect = rect.ContractedBy(10f);
            
            // 顶部工具栏
            Rect toolbarRect = innerRect.TopPartPixels(30f);
            if (Widgets.ButtonText(toolbarRect.RightPartPixels(120f), "RTE_Area_NewArea".Translate()))
            {
                var map = Find.CurrentMap;
                if (map != null)
                {
                    var newArea = new RimTalkHealthEnhance.Models.CustomNamedArea(map, "RTE_Area_NewAreaName".Translate());
                    manager.AddCustomArea(newArea);
                    Find.WindowStack.Add(new RimTalkHealthEnhance.UI.AreaEditorDialog(newArea, true));
                }
                else
                {
                    Messages.Message("RTE_Area_EnterMapFirst".Translate(), MessageTypeDefOf.RejectInput, false);
                }
            }
            
            // 列表区域
            Rect listRect = new Rect(innerRect.x, toolbarRect.yMax + 10f, innerRect.width, innerRect.height - 40f);
            
            if (manager.CustomAreas == null || !manager.CustomAreas.Any())
            {
                Widgets.Label(listRect, "RTE_Area_NoData".Translate());
                return;
            }
            
            // 虚拟化渲染 - 只绘制可见区域内的项
            float itemHeight = 60f;
            float gap = 5f;
            float totalHeight = manager.CustomAreas.Count * (itemHeight + gap);
            float listWidth = listRect.width - 16f; // 减去滚动条宽度
            
            Rect viewRect = new Rect(0, 0, listWidth, totalHeight);
            Widgets.BeginScrollView(listRect, ref areaScrollPos, viewRect);
            
            // 虚拟化：只渲染可见区域
            float scrollY = areaScrollPos.y;
            float viewHeight = listRect.height;
            float currentY = 0f;
            
            foreach (var area in manager.CustomAreas)
            {
                // 只绘制可见区域内的项
                if (currentY + itemHeight >= scrollY && currentY <= scrollY + viewHeight)
                {
                    Rect itemRect = new Rect(0, currentY, listWidth, itemHeight);
                    DrawCustomAreaItem(itemRect, area, manager);
                }
                
                currentY += itemHeight + gap;
                
                // 提前退出优化：如果已经超出可视区域，不再继续遍历
                if (currentY > scrollY + viewHeight) break;
            }
            
            Widgets.EndScrollView();
        }
        
        private void DrawCustomAreaItem(Rect rect, RimTalkHealthEnhance.Models.CustomNamedArea area, ColonyAnnouncementManager manager)
        {
            Widgets.DrawOptionBackground(rect, false);
            if (Mouse.IsOver(rect)) Widgets.DrawHighlight(rect);
            
            Rect contentRect = rect.ContractedBy(6f);
            
            // 颜色指示器
            Widgets.DrawBoxSolid(new Rect(contentRect.x, contentRect.y, 4f, contentRect.height), area.Color);
            contentRect.xMin += 10f;
            
            // 信息区域
            Rect infoRect = contentRect.LeftPartPixels(contentRect.width - 300f);
            Rect btnRect = contentRect.RightPartPixels(290f);
            
            // 区域名称
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(infoRect.TopHalf(), area.Label);
            
            // 统计信息
            Text.Font = GameFont.Tiny;
            GUI.color = Color.gray;
            Widgets.Label(infoRect.BottomHalf(), $"格子数: {area.CellCount}  |  状态: {(area.IsActive ? "启用" : "禁用")}");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 按钮区域
            float btnY = btnRect.y + (btnRect.height - 24f) / 2f;
            float btnW = 70f;
            float gap = 4f;
            
            // 绘制按钮
            if (Widgets.ButtonText(new Rect(btnRect.x, btnY, btnW, 24f), "RTE_Area_Draw".Translate()))
            {
                var designator = new RimTalkHealthEnhance.UI.AreaDrawingDesignator();
                designator.StartDrawing(area, true);
            }
            
            // 移除按钮
            if (Widgets.ButtonText(new Rect(btnRect.x + btnW + gap, btnY, btnW, 24f), "RTE_Area_RemoveCells".Translate()))
            {
                var designator = new RimTalkHealthEnhance.UI.AreaDrawingDesignator();
                designator.StartDrawing(area, false);
            }
            
            // 编辑按钮
            if (Widgets.ButtonText(new Rect(btnRect.x + (btnW + gap) * 2, btnY, btnW, 24f), "RTE_Announcement_Edit".Translate()))
            {
                Find.WindowStack.Add(new RimTalkHealthEnhance.UI.AreaEditorDialog(area));
            }
            
            // 删除按钮
            if (Widgets.ButtonText(new Rect(btnRect.x + (btnW + gap) * 3, btnY, btnW, 24f), "RTE_Announcement_Delete".Translate()))
            {
                // 捕获 ID 以避免闭包问题
                string areaId = area.Id;
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                    "RTE_Area_ConfirmDelete".Translate(area.Label),
                    () =>
                    {
                        manager.DeleteCustomArea(areaId);
                    }
                ));
            }
        }
        
        /// <summary>
        /// 显示殖民者选择菜单，用于发起讨论（带模式选择）
        /// </summary>
        private void ShowPawnSelectorMenu(ColonyAnnouncement item)
        {
            // 检查DiscussionService是否可用
            if (!DiscussionService.IsAvailable())
            {
                Messages.Message("RTE_Announcement_Discuss_Failed".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            // 模式一：玩家发起讨论
            options.Add(new FloatMenuOption(
                "RTE_Announcement_Discuss_PlayerMode".Translate(),
                () => ShowPawnSelectorForPlayerMode(item)
            ));
            
            // 模式二：小人自己讨论
            options.Add(new FloatMenuOption(
                "RTE_Announcement_Discuss_PawnMode".Translate(),
                () => ShowPawnSelectorForPawnMode(item)
            ));
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        /// <summary>
        /// 显示殖民者选择菜单 - 玩家发起讨论模式
        /// </summary>
        private void ShowPawnSelectorForPlayerMode(ColonyAnnouncement item)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            // 随机选项
            options.Add(new FloatMenuOption(
                "RTE_Announcement_Discuss_Random".Translate(),
                () =>
                {
                    var pawn = DiscussionService.SelectRandomColonist();
                    if (pawn != null)
                    {
                        DiscussionService.StartDiscussion(pawn, item);
                    }
                    else
                    {
                        Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
            ));
            
            // 获取可用的殖民者列表
            var colonists = DiscussionService.GetAvailableColonists();
            
            if (colonists.Any())
            {
                // 添加分隔线（用一个禁用的选项模拟）
                options.Add(new FloatMenuOption("---", null) { Disabled = true });
                
                // 具体殖民者选项
                foreach (var pawn in colonists)
                {
                    var p = pawn; // 避免闭包问题
                    options.Add(new FloatMenuOption(
                        p.LabelShortCap,
                        () => DiscussionService.StartDiscussion(p, item)
                    ));
                }
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
        /// <summary>
        /// 显示殖民者选择菜单 - 小人自己讨论模式
        /// </summary>
        private void ShowPawnSelectorForPawnMode(ColonyAnnouncement item)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            // 随机选项
            options.Add(new FloatMenuOption(
                "RTE_Announcement_Discuss_Random".Translate(),
                () =>
                {
                    var pawn = DiscussionService.SelectRandomColonist();
                    if (pawn != null)
                    {
                        DiscussionService.StartPawnSelfDiscussion(pawn, item);
                    }
                    else
                    {
                        Messages.Message("RTE_Announcement_Discuss_NoColonist".Translate(), MessageTypeDefOf.RejectInput, false);
                    }
                }
            ));
            
            // 获取可用的殖民者列表
            var colonists = DiscussionService.GetAvailableColonists();
            
            if (colonists.Any())
            {
                // 添加分隔线（用一个禁用的选项模拟）
                options.Add(new FloatMenuOption("---", null) { Disabled = true });
                
                // 具体殖民者选项
                foreach (var pawn in colonists)
                {
                    var p = pawn; // 避免闭包问题
                    options.Add(new FloatMenuOption(
                        p.LabelShortCap,
                        () => DiscussionService.StartPawnSelfDiscussion(p, item)
                    ));
                }
            }
            
            Find.WindowStack.Add(new FloatMenu(options));
        }
        
    }
}
