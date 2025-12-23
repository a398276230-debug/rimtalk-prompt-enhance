using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using RimWorld;
using RimTalkHealthEnhance.Models;
using RimTalkHealthEnhance.UI;

namespace RimTalkHealthEnhance
{
    public class TaskEditorDialog : Window
    {
        private ColonyAnnouncement announcement;
        private ColonyAnnouncementManager manager;
        private bool isNew;
        
        private string editTitle;
        private string editDescription;
        private AnnouncementCategory editCategory;
        private AnnouncementPriority editPriority;
        private AnnouncementStatus editStatus;
        
        // Optional fields
        private float editProgress;
        private string editAssignedPawn;
        
        // 施工区域相关
        private bool isGeneratingAISummary = false;
        
        public TaskEditorDialog(ColonyAnnouncement existing, ColonyAnnouncementManager mgr)
        {
            manager = mgr;
            isNew = (existing == null);
            
            if (isNew)
            {
                announcement = new ColonyAnnouncement
                {
                    Id = Guid.NewGuid().ToString(),
                    CreatedTick = Find.TickManager.TicksGame,
                    Status = AnnouncementStatus.Active,
                    Priority = AnnouncementPriority.Normal,
                    Category = AnnouncementCategory.Project
                };
                editTitle = "";
                editDescription = "";
                editCategory = AnnouncementCategory.Project;
                editPriority = AnnouncementPriority.Normal;
                editStatus = AnnouncementStatus.Active;
                editProgress = 0f;
                editAssignedPawn = "";
            }
            else
            {
                announcement = existing;
                editTitle = announcement.Title;
                editDescription = announcement.Description;
                editCategory = announcement.Category;
                editPriority = announcement.Priority;
                editStatus = announcement.Status;
                editProgress = announcement.Progress;
                editAssignedPawn = announcement.AssignedPawnName;
                
                // 如果有施工区域，重新扫描蓝图数量
                if (!string.IsNullOrEmpty(announcement.BlueprintAreaId))
                {
                    var area = manager.CustomAreas?.FirstOrDefault(a => a.Id == announcement.BlueprintAreaId);
                    if (area != null)
                    {
                        int currentCount = BlueprintProgressService.CountBlueprintsInArea(area);
                        
                        // 如果是第一次扫描（InitialBlueprintCount为0），设置初始值
                        if (announcement.InitialBlueprintCount == 0)
                        {
                            announcement.InitialBlueprintCount = currentCount;
                        }
                        
                        // 更新进度
                        if (announcement.AutoCalculateProgress)
                        {
                            announcement.Progress = BlueprintProgressService.CalculateProgress(
                                announcement.InitialBlueprintCount, 
                                currentCount
                            );
                            editProgress = announcement.Progress;
                        }
                    }
                }
            }
            
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }
        
        public override Vector2 InitialSize => new Vector2(500f, 750f);
        
        public override void DoWindowContents(Rect inRect)
        {
            // 检测回车键
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                Event.current.Use();
                SaveAndClose();
                return;
            }
            
            // 为底部按钮预留空间
            Rect contentRect = inRect.TopPartPixels(inRect.height - 50f);
            Rect buttonRect = new Rect(inRect.x, inRect.yMax - 45f, inRect.width, 40f);
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(contentRect);
            
            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), isNew ? "新建状况" : "编辑状况");
            Text.Font = GameFont.Small;
            listing.GapLine();
            listing.Gap();
            
            // 类别
            Rect catRect = listing.GetRect(30f);
            Widgets.Label(catRect.LeftHalf(), "类别：");
            if (Widgets.ButtonText(catRect.RightHalf(), editCategory.ToString()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (AnnouncementCategory c in Enum.GetValues(typeof(AnnouncementCategory)))
                {
                    options.Add(new FloatMenuOption(c.ToString(), () => editCategory = c));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.Gap();
            
            // 标题
            Widgets.Label(listing.GetRect(24f), "标题：");
            editTitle = listing.TextEntry(editTitle);
            listing.Gap();
            
            // 描述
            Widgets.Label(listing.GetRect(24f), "描述：");
            Rect descRect = listing.GetRect(100f);
            editDescription = Widgets.TextArea(descRect, editDescription);
            listing.Gap();
            
            // 优先级
            Rect priorityRect = listing.GetRect(30f);
            Widgets.Label(priorityRect.LeftHalf(), "优先级：");
            
            string priorityLabel = editPriority.ToString();
            Color priorityColor = editPriority switch
            {
                AnnouncementPriority.Urgent => new Color(1f, 0.2f, 0.2f),
                AnnouncementPriority.High => new Color(1f, 0.6f, 0f),
                AnnouncementPriority.Normal => new Color(0.2f, 0.8f, 0.2f),
                _ => Color.white
            };
            
            GUI.color = priorityColor;
            if (Widgets.ButtonText(priorityRect.RightHalf(), priorityLabel))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (AnnouncementPriority p in Enum.GetValues(typeof(AnnouncementPriority)))
                {
                    options.Add(new FloatMenuOption(p.ToString(), () => editPriority = p));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            GUI.color = Color.white;
            listing.Gap();
            
            // 状态
            if (!isNew)
            {
                Rect statusRect = listing.GetRect(30f);
                Widgets.Label(statusRect.LeftHalf(), "状态：");
                if (Widgets.ButtonText(statusRect.RightHalf(), editStatus.ToString()))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    foreach (AnnouncementStatus s in Enum.GetValues(typeof(AnnouncementStatus)))
                    {
                        options.Add(new FloatMenuOption(s.ToString(), () => editStatus = s));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
            }
            
            listing.GapLine();
            listing.Gap();
            
            // === 特定类别字段 ===
            if (editCategory == AnnouncementCategory.Project)
            {
                // 进度显示
                bool hasArea = !string.IsNullOrEmpty(announcement.BlueprintAreaId);
                string progressLabel = hasArea && announcement.AutoCalculateProgress 
                    ? $"进度: {editProgress:P0} (自动计算)" 
                    : $"进度: {editProgress:P0}";
                
                Widgets.Label(listing.GetRect(24f), progressLabel);
                
                // 如果没有启用自动计算，显示手动滑块
                if (!announcement.AutoCalculateProgress)
                {
                    editProgress = listing.Slider(editProgress, 0f, 1f);
                }
                else
                {
                    // 显示禁用的滑块（只读）
                    GUI.enabled = false;
                    listing.Slider(editProgress, 0f, 1f);
                    GUI.enabled = true;
                }
                listing.Gap();
                
                // 负责人
                Widgets.Label(listing.GetRect(24f), "负责人：");
                if (Widgets.ButtonText(listing.GetRect(30f), string.IsNullOrEmpty(editAssignedPawn) ? "无" : editAssignedPawn))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("无", () => editAssignedPawn = ""));
                    
                    foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonists)
                    {
                        options.Add(new FloatMenuOption(pawn.LabelShort, () => editAssignedPawn = pawn.LabelShort));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
                
                // 施工区域按钮
                listing.GapLine();
                listing.Gap();
                
                Rect areaButtonRect = listing.GetRect(30f);
                if (hasArea)
                {
                    var area = manager.CustomAreas?.FirstOrDefault(a => a.Id == announcement.BlueprintAreaId);
                    string areaLabel = area != null 
                        ? $"施工区域: {area.Label} ({announcement.InitialBlueprintCount} 蓝图)" 
                        : "施工区域: (已删除)";
                    
                    if (Widgets.ButtonText(areaButtonRect.LeftHalf(), areaLabel))
                    {
                        // 重新框选区域
                        StartAreaSelection();
                    }
                    
                    if (Widgets.ButtonText(areaButtonRect.RightHalf(), "移除区域"))
                    {
                        RemoveConstructionArea();
                    }
                }
                else
                {
                    if (Widgets.ButtonText(areaButtonRect, "框选施工区域"))
                    {
                        StartAreaSelection();
                    }
                }
                listing.Gap();
                
                // AI 总结按钮
                Rect aiButtonRect = listing.GetRect(30f);
                GUI.enabled = hasArea && !isGeneratingAISummary;
                string aiButtonLabel = isGeneratingAISummary ? "生成中..." : "AI 总结工程";
                if (Widgets.ButtonText(aiButtonRect.LeftHalf(), aiButtonLabel))
                {
                    _ = GenerateAISummary();
                }
                GUI.enabled = true;
                
                // 自定义提示词按钮
                if (Widgets.ButtonText(aiButtonRect.RightHalf(), "自定义提示词"))
                {
                    string defaultPrompt = @"请根据以下建筑蓝图信息，为这个工程项目生成一个简洁的名称和描述。

蓝图列表：
{blueprintList}

要求：
1. 名称：不超过15个字，概括工程的主要目的
2. 描述：不超过50个字，说明工程的具体内容和目标
3. 使用中文
4. 保持专业和简洁

请以JSON格式返回：
{
  ""title"": ""工程名称"",
  ""description"": ""工程描述""
}";

                    Find.WindowStack.Add(new PromptEditorDialog(
                        "工程AI总结提示词",
                        RimTalkHealthEnhanceMod.Settings.CustomProjectSummaryPrompt,
                        defaultPrompt,
                        (newPrompt) => {
                            RimTalkHealthEnhanceMod.Settings.CustomProjectSummaryPrompt = newPrompt;
                            RimTalkHealthEnhanceMod.Settings.Write();
                        }
                    ));
                }
                listing.Gap();
            }
            else if (editCategory == AnnouncementCategory.Personnel)
            {
                Widgets.Label(listing.GetRect(24f), "相关人员：");
                if (Widgets.ButtonText(listing.GetRect(30f), string.IsNullOrEmpty(editAssignedPawn) ? "无" : editAssignedPawn))
                {
                    List<FloatMenuOption> options = new List<FloatMenuOption>();
                    options.Add(new FloatMenuOption("无", () => editAssignedPawn = ""));
                    
                    foreach (var pawn in Find.CurrentMap.mapPawns.FreeColonists)
                    {
                        options.Add(new FloatMenuOption(pawn.LabelShort, () => editAssignedPawn = pawn.LabelShort));
                    }
                    Find.WindowStack.Add(new FloatMenu(options));
                }
                listing.Gap();
            }
            
            listing.End();
            
            // 保存/取消按钮（固定在底部）
            if (Widgets.ButtonText(buttonRect.LeftHalf().ContractedBy(5f), "保存 (Enter)"))
            {
                SaveAndClose();
            }
            
            if (Widgets.ButtonText(buttonRect.RightHalf().ContractedBy(5f), "取消 (Esc)"))
            {
                Close();
            }
        }
        
        private void SaveAndClose()
        {
            announcement.Title = editTitle;
            announcement.Description = editDescription;
            announcement.Category = editCategory;
            announcement.Priority = editPriority;
            announcement.Status = editStatus;
            announcement.Progress = editProgress;
            announcement.AssignedPawnName = editAssignedPawn;
            
            if (isNew)
            {
                manager.AddAnnouncement(announcement);
            }
            
            Close();
        }
        
        /// <summary>
        /// 开始框选施工区域
        /// </summary>
        private void StartAreaSelection()
        {
            // 如果是新建，先保存工程
            if (isNew)
            {
                // 如果没有标题，给一个默认标题
                if (string.IsNullOrEmpty(editTitle))
                {
                    editTitle = "未命名工程";
                }
                
                announcement.Title = editTitle;
                announcement.Description = editDescription;
                announcement.Category = editCategory;
                announcement.Priority = editPriority;
                announcement.Status = editStatus;
                announcement.Progress = editProgress;
                announcement.AssignedPawnName = editAssignedPawn;
                
                manager.AddAnnouncement(announcement);
                isNew = false; // 标记为已保存
            }
            
            // 关闭当前对话框
            Close();
            
            // 创建或获取施工区域
            CustomNamedArea area;
            if (!string.IsNullOrEmpty(announcement.BlueprintAreaId))
            {
                // 已有区域，重新框选
                area = manager.CustomAreas?.FirstOrDefault(a => a.Id == announcement.BlueprintAreaId);
                if (area != null)
                {
                    area.Clear(); // 清空现有区域
                }
                else
                {
                    // 区域已被删除，创建新的
                    area = new CustomNamedArea(Find.CurrentMap, $"{announcement.Title} 施工区");
                    area.IsConstructionArea = true;
                    announcement.BlueprintAreaId = area.Id;
                    manager.AddCustomArea(area);
                }
            }
            else
            {
                // 新建区域
                string areaName = string.IsNullOrEmpty(announcement.Title) 
                    ? "施工区域" 
                    : $"{announcement.Title} 施工区";
                area = new CustomNamedArea(Find.CurrentMap, areaName);
                area.IsConstructionArea = true;
                announcement.BlueprintAreaId = area.Id;
                manager.AddCustomArea(area);
            }
            
            // 启动绘制工具
            var designator = new AreaDrawingDesignator();
            designator.StartDrawing(area, true);
            
            // 显示提示消息
            Messages.Message(
                "拖拽鼠标框选施工区域。完成后会自动扫描蓝图数量。",
                MessageTypeDefOf.NeutralEvent,
                false
            );
            
            // 注意：不在这里扫描，因为玩家还没有绘制完成
            // 蓝图数量将在玩家重新打开编辑窗口时更新
            announcement.AutoCalculateProgress = true;
            announcement.Progress = 0f;
        }
        
        /// <summary>
        /// 移除施工区域
        /// </summary>
        private void RemoveConstructionArea()
        {
            if (!string.IsNullOrEmpty(announcement.BlueprintAreaId))
            {
                manager.DeleteCustomArea(announcement.BlueprintAreaId);
                announcement.BlueprintAreaId = null;
                announcement.InitialBlueprintCount = 0;
                announcement.AutoCalculateProgress = false;
                
                Messages.Message("已移除施工区域", MessageTypeDefOf.NeutralEvent, false);
            }
        }
        
        /// <summary>
        /// 生成AI总结
        /// </summary>
        private async Task GenerateAISummary()
        {
            if (string.IsNullOrEmpty(announcement.BlueprintAreaId))
            {
                Messages.Message("请先框选施工区域", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            var area = manager.CustomAreas?.FirstOrDefault(a => a.Id == announcement.BlueprintAreaId);
            if (area == null)
            {
                Messages.Message("施工区域不存在", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            isGeneratingAISummary = true;
            
            try
            {
                // 获取蓝图详情
                var blueprints = BlueprintProgressService.GetBlueprintDetailsInArea(area);
                
                if (blueprints.Count == 0)
                {
                    Messages.Message("施工区域内没有蓝图", MessageTypeDefOf.RejectInput, false);
                    isGeneratingAISummary = false;
                    return;
                }
                
                // 构建提示词
                string blueprintList = string.Join("\n", blueprints.Select(b => $"- {b}"));
                
                // 使用自定义提示词（如果有）
                var settings = RimTalkHealthEnhanceMod.Settings;
                string prompt;
                
                if (!string.IsNullOrEmpty(settings.CustomProjectSummaryPrompt))
                {
                    // 使用自定义提示词，替换 {blueprintList} 占位符
                    prompt = settings.CustomProjectSummaryPrompt.Replace("{blueprintList}", blueprintList);
                }
                else
                {
                    // 使用默认提示词
                    prompt = $@"请根据以下建筑蓝图信息，为这个工程项目生成一个简洁的名称和描述。

蓝图列表：
{blueprintList}

要求：
1. 名称：不超过15个字，概括工程的主要目的
2. 描述：不超过50个字，说明工程的具体内容和目标
3. 使用中文
4. 保持专业和简洁

请以JSON格式返回：
{{
  ""title"": ""工程名称"",
  ""description"": ""工程描述""
}}";
                }

                // 调用AI（使用现有的 SimpleAIClient.CallAI）
                string response = await SimpleAIClient.CallAI(prompt);
                
                if (string.IsNullOrEmpty(response))
                {
                    Messages.Message("AI调用失败，请检查API配置", MessageTypeDefOf.RejectInput, false);
                    isGeneratingAISummary = false;
                    return;
                }
                
                // 解析JSON响应
                try
                {
                    // 尝试提取JSON部分（可能被markdown代码块包裹）
                    string jsonText = response.Trim();
                    
                    // 移除可能的markdown代码块标记
                    if (jsonText.StartsWith("```json"))
                    {
                        jsonText = jsonText.Substring(7);
                    }
                    else if (jsonText.StartsWith("```"))
                    {
                        jsonText = jsonText.Substring(3);
                    }
                    
                    if (jsonText.EndsWith("```"))
                    {
                        jsonText = jsonText.Substring(0, jsonText.Length - 3);
                    }
                    
                    jsonText = jsonText.Trim();
                    
                    // 尝试解析JSON
                    var json = Newtonsoft.Json.Linq.JObject.Parse(jsonText);
                    string title = json["title"]?.ToString();
                    string description = json["description"]?.ToString();
                    
                    if (!string.IsNullOrEmpty(title))
                    {
                        editTitle = title;
                        announcement.Title = title;
                    }
                    
                    if (!string.IsNullOrEmpty(description))
                    {
                        editDescription = description;
                        announcement.Description = description;
                    }
                    
                    Messages.Message("AI总结完成", MessageTypeDefOf.PositiveEvent, false);
                }
                catch (Exception parseEx)
                {
                    Log.Warning($"[RimTalk Enhance] JSON解析失败: {parseEx.Message}\n原始响应: {response}");
                    
                    // 如果JSON解析失败，尝试直接使用响应文本
                    if (response.Length > 0 && response.Length < 100)
                    {
                        editTitle = response.Substring(0, Math.Min(30, response.Length));
                        announcement.Title = editTitle;
                        Messages.Message("AI响应格式异常，已使用原始文本", MessageTypeDefOf.CautionInput, false);
                    }
                    else
                    {
                        Messages.Message($"AI响应格式异常：{parseEx.Message}", MessageTypeDefOf.RejectInput, false);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] AI总结失败: {ex.Message}");
                Messages.Message($"AI总结失败: {ex.Message}", MessageTypeDefOf.RejectInput, false);
            }
            finally
            {
                isGeneratingAISummary = false;
            }
        }
    }
}
