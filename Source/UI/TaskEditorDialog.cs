using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

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
            }
            
            doCloseX = true;
            forcePause = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
        }
        
        public override Vector2 InitialSize => new Vector2(500f, 600f);
        
        public override void DoWindowContents(Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            
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
                Widgets.Label(listing.GetRect(24f), $"进度: {editProgress:P0}");
                editProgress = listing.Slider(editProgress, 0f, 1f);
                listing.Gap();
                
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
            
            listing.GapLine();
            listing.Gap();
            
            // 保存/取消按钮
            Rect buttonRow = listing.GetRect(40f);
            if (Widgets.ButtonText(buttonRow.LeftHalf().ContractedBy(5f), "保存"))
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
            
            if (Widgets.ButtonText(buttonRow.RightHalf().ContractedBy(5f), "取消"))
            {
                Close();
            }
            
            listing.End();
        }
    }
}
