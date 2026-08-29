using System;
using System.Collections.Generic;
using System.Linq;
using RimTalkHealthEnhance.Services;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance.UI
{
    /// <summary>
    /// 群体讨论选择弹窗
    /// 允许用户选择发起者（领导者）和参与者
    /// </summary>
    public class GroupDiscussionDialog : Window
    {
        private readonly ColonyAnnouncement _item;
        private readonly List<Pawn> _availableColonists;
        private List<Pawn> _participantCandidates;
        private Pawn _selectedLeader;
        private HashSet<Pawn> _selectedParticipants;
        
        private Vector2 _leaderScrollPosition;
        private Vector2 _participantScrollPosition;
        
        private const float RowHeight = 30f;
        private const float Margin = 10f;
        private const float ButtonHeight = 35f;
        
        public override Vector2 InitialSize => new Vector2(600f, 500f);
        
        public GroupDiscussionDialog(ColonyAnnouncement item)
        {
            _item = item;
            _availableColonists = GroupDiscussionService.GetAvailableColonists();
            _selectedParticipants = new HashSet<Pawn>();
            
            // 默认选择第一个作为领导者
            if (_availableColonists.Count > 0)
            {
                _selectedLeader = _availableColonists[0];
            }

            // 参与者候选依赖领导者，须在默认领导者确定后构建
            RefreshParticipantCandidates();

            doCloseButton = false;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
            forcePause = true;
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            // 标题
            Text.Font = GameFont.Medium;
            string title = "RTE_GroupDiscussion_Title".Translate(_item.Title);
            Rect titleRect = new Rect(0f, 0f, inRect.width, 35f);
            Widgets.Label(titleRect, title);
            Text.Font = GameFont.Small;
            
            float curY = 40f;
            
            // 警告信息
            Rect warningRect = new Rect(0f, curY, inRect.width, 40f);
            GUI.color = new Color(1f, 0.8f, 0.2f);
            Text.Font = GameFont.Small;
            string warning = "RTE_GroupDiscussion_Warning".Translate();
            Widgets.Label(warningRect, warning);
            GUI.color = Color.white;
            curY += 45f;
            
            // 检查是否有可用殖民者
            if (_availableColonists.Count == 0)
            {
                Rect noColonistRect = new Rect(0f, curY, inRect.width, 30f);
                Widgets.Label(noColonistRect, "RTE_Announcement_Discuss_NoColonist".Translate());
                curY = inRect.height - ButtonHeight - Margin;
                DrawCloseButton(new Rect(0f, curY, inRect.width, ButtonHeight));
                return;
            }
            
            if (_availableColonists.Count < 2)
            {
                Rect notEnoughRect = new Rect(0f, curY, inRect.width, 30f);
                Widgets.Label(notEnoughRect, "RTE_GroupDiscussion_NotEnough".Translate());
                curY = inRect.height - ButtonHeight - Margin;
                DrawCloseButton(new Rect(0f, curY, inRect.width, ButtonHeight));
                return;
            }
            
            // 分割线
            curY += 5f;
            Widgets.DrawLineHorizontal(0f, curY, inRect.width);
            curY += 10f;
            
            // 左右两列的区域
            float columnWidth = (inRect.width - Margin * 3) / 2f;
            float listHeight = inRect.height - curY - ButtonHeight - Margin * 2 - 30f;
            
            // 左列：发起者选择
            Rect leaderLabelRect = new Rect(Margin, curY, columnWidth, 25f);
            Widgets.Label(leaderLabelRect, "RTE_GroupDiscussion_Leader".Translate());
            
            // 右列：参与者选择
            Rect participantLabelRect = new Rect(Margin * 2 + columnWidth, curY, columnWidth, 25f);
            Widgets.Label(participantLabelRect, "RTE_GroupDiscussion_Participants".Translate());
            
            curY += 30f;
            
            // 左列：发起者列表
            Rect leaderListRect = new Rect(Margin, curY, columnWidth, listHeight);
            DrawLeaderSelection(leaderListRect);
            
            // 右列：参与者列表
            Rect participantListRect = new Rect(Margin * 2 + columnWidth, curY, columnWidth, listHeight);
            DrawParticipantSelection(participantListRect);
            
            curY += listHeight + Margin;
            
            // 统计信息
            Rect statsRect = new Rect(Margin * 2 + columnWidth, curY - 25f, columnWidth, 20f);
            int participantCount = _selectedParticipants.Count;
            string statsText = "RTE_GroupDiscussion_Selected".Translate(participantCount);
            Text.Anchor = TextAnchor.MiddleRight;
            Widgets.Label(statsRect, statsText);
            Text.Anchor = TextAnchor.UpperLeft;
            
            // 底部按钮
            Rect footerRect = new Rect(0f, inRect.height - ButtonHeight, inRect.width, ButtonHeight);
            DrawFooter(footerRect);
        }
        
        private void DrawLeaderSelection(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawBox(rect);
            
            Rect innerRect = rect.ContractedBy(5f);
            float viewHeight = _availableColonists.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, viewHeight);
            
            Widgets.BeginScrollView(innerRect, ref _leaderScrollPosition, viewRect);
            
            float y = 0f;
            foreach (var pawn in _availableColonists)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                bool isSelected = _selectedLeader == pawn;
                
                // 高亮选中
                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }
                
                // 单选按钮
                Rect radioRect = new Rect(5f, y + 5f, 20f, 20f);
                if (Widgets.RadioButton(radioRect.x, radioRect.y, isSelected))
                {
                    if (_selectedLeader != pawn)
                    {
                        _selectedLeader = pawn;
                        // 如果新选择的领导者之前在参与者列表中，移除它
                        _selectedParticipants.Remove(pawn);
                        RefreshParticipantCandidates();
                    }
                }
                
                // 殖民者名称和状态
                Rect labelRect = new Rect(30f, y, viewRect.width - 35f, RowHeight);
                string label = GetPawnLabel(pawn);
                
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
                
                // 点击整行也可选择
                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (_selectedLeader != pawn)
                    {
                        _selectedLeader = pawn;
                        _selectedParticipants.Remove(pawn);
                        RefreshParticipantCandidates();
                    }
                }
                
                y += RowHeight;
            }
            
            Widgets.EndScrollView();
        }
        
        /// <summary>
        /// 领导者变更后重建参与者候选（与上游 Announcement 聚集规则一致），并剔除已不在候选中的选中项
        /// </summary>
        private void RefreshParticipantCandidates()
        {
            _participantCandidates = _selectedLeader == null
                ? new List<Pawn>()
                : GroupDiscussionService.GetAvailableColonists(_selectedLeader);

            _selectedParticipants.RemoveWhere(p => !_participantCandidates.Contains(p));
        }

        private void DrawParticipantSelection(Rect rect)
        {
            Widgets.DrawBoxSolid(rect, new Color(0.1f, 0.1f, 0.1f, 0.5f));
            Widgets.DrawBox(rect);

            // 过滤掉领导者（上游已排除，双保险）
            var availableParticipants = _participantCandidates.Where(p => p != _selectedLeader).ToList();

            Rect innerRect = rect.ContractedBy(5f);

            // 过滤后候选为空：提示无可参与者
            if (availableParticipants.Count == 0)
            {
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(innerRect, "RTE_GroupDiscussion_NoParticipants".Translate());
                Text.Anchor = TextAnchor.UpperLeft;
                return;
            }

            float viewHeight = availableParticipants.Count * RowHeight;
            Rect viewRect = new Rect(0f, 0f, innerRect.width - 16f, viewHeight);
            
            Widgets.BeginScrollView(innerRect, ref _participantScrollPosition, viewRect);
            
            float y = 0f;
            foreach (var pawn in availableParticipants)
            {
                Rect rowRect = new Rect(0f, y, viewRect.width, RowHeight);
                bool isSelected = _selectedParticipants.Contains(pawn);
                
                // 高亮
                if (isSelected)
                {
                    Widgets.DrawHighlightSelected(rowRect);
                }
                else if (Mouse.IsOver(rowRect))
                {
                    Widgets.DrawHighlight(rowRect);
                }
                
                // 复选框
                Rect checkRect = new Rect(5f, y + 5f, 20f, 20f);
                bool newSelected = isSelected;
                Widgets.Checkbox(checkRect.x, checkRect.y, ref newSelected);
                
                if (newSelected != isSelected)
                {
                    if (newSelected)
                        _selectedParticipants.Add(pawn);
                    else
                        _selectedParticipants.Remove(pawn);
                }
                
                // 殖民者名称和状态
                Rect labelRect = new Rect(30f, y, viewRect.width - 35f, RowHeight);
                string label = GetPawnLabel(pawn);
                
                Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(labelRect, label);
                Text.Anchor = TextAnchor.UpperLeft;
                
                // 点击整行也可切换选择
                if (Widgets.ButtonInvisible(rowRect))
                {
                    if (_selectedParticipants.Contains(pawn))
                        _selectedParticipants.Remove(pawn);
                    else
                        _selectedParticipants.Add(pawn);
                }
                
                y += RowHeight;
            }
            
            Widgets.EndScrollView();
        }
        
        private string GetPawnLabel(Pawn pawn)
        {
            string label = pawn.LabelShortCap;
            
            // 添加职业/角色信息
            if (pawn.story?.title != null)
            {
                label += $" ({pawn.story.title})";
            }
            
            return label;
        }
        
        private void DrawFooter(Rect rect)
        {
            float buttonWidth = 120f;
            float buttonSpacing = 10f;
            float totalButtonWidth = buttonWidth * 4 + buttonSpacing * 3;
            float startX = (rect.width - totalButtonWidth) / 2f;
            
            // 全选按钮
            Rect selectAllRect = new Rect(startX, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(selectAllRect, "RTE_GroupDiscussion_SelectAll".Translate()))
            {
                foreach (var pawn in _participantCandidates)
                {
                    if (pawn != _selectedLeader)
                        _selectedParticipants.Add(pawn);
                }
            }
            
            // 取消全选按钮
            Rect deselectAllRect = new Rect(startX + buttonWidth + buttonSpacing, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(deselectAllRect, "RTE_GroupDiscussion_DeselectAll".Translate()))
            {
                _selectedParticipants.Clear();
            }
            
            // 确认按钮
            Rect confirmRect = new Rect(startX + (buttonWidth + buttonSpacing) * 2, rect.y, buttonWidth, rect.height);
            bool canConfirm = _selectedLeader != null && _selectedParticipants.Count > 0;
            
            GUI.color = canConfirm ? Color.white : Color.gray;
            if (Widgets.ButtonText(confirmRect, "RTE_GroupDiscussion_Confirm".Translate()) && canConfirm)
            {
                StartDiscussion();
            }
            GUI.color = Color.white;
            
            // 取消按钮
            Rect cancelRect = new Rect(startX + (buttonWidth + buttonSpacing) * 3, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(cancelRect, "RTE_GroupDiscussion_Cancel".Translate()))
            {
                Close();
            }
        }
        
        private void DrawCloseButton(Rect rect)
        {
            float buttonWidth = 120f;
            Rect closeRect = new Rect((rect.width - buttonWidth) / 2f, rect.y, buttonWidth, rect.height);
            if (Widgets.ButtonText(closeRect, "RTE_GroupDiscussion_Cancel".Translate()))
            {
                Close();
            }
        }
        
        private void StartDiscussion()
        {
            if (_selectedLeader == null)
            {
                Messages.Message("RTE_GroupDiscussion_NoLeader".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            if (_selectedParticipants.Count == 0)
            {
                Messages.Message("RTE_GroupDiscussion_NoParticipants".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            // 发起群体讨论
            bool success = GroupDiscussionService.StartGroupDiscussion(
                _item,
                _selectedLeader,
                _selectedParticipants.ToList()
            );
            
            if (success)
            {
                Close();
            }
        }
    }
}