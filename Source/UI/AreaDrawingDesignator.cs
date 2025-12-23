using RimTalkHealthEnhance.Models;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance.UI
{
    /// <summary>
    /// 自定义区域绘制工具 - 参考 Planning Expand 的 Designator 实现
    /// </summary>
    public class AreaDrawingDesignator : Designator
    {
        private CustomNamedArea currentArea;
        private bool isAdding = true; // true=添加格子, false=移除格子
        
        // 关键属性：启用拖拽功能
        public override DrawStyleCategoryDef DrawStyleCategory => DrawStyleCategoryDefOf.FilledRectangle;
        public override bool DragDrawMeasurements => true;
        
        public AreaDrawingDesignator()
        {
            defaultLabel = "绘制区域";
            defaultDesc = "拖拽鼠标绘制或移除区域格子";
            icon = ContentFinder<Texture2D>.Get("UI/Designators/PlanningZoneExpand", false) ?? BaseContent.WhiteTex;
            soundDragSustain = SoundDefOf.Designate_DragStandard;
            soundDragChanged = SoundDefOf.Designate_DragStandard_Changed;
            useMouseIcon = true;
        }
        
        /// <summary>
        /// 开始绘制指定区域
        /// </summary>
        public void StartDrawing(CustomNamedArea area, bool adding)
        {
            currentArea = area;
            isAdding = adding;
            Find.DesignatorManager.Select(this);
        }
        
        /// <summary>
        /// 停止绘制
        /// </summary>
        public void StopDrawing()
        {
            Find.DesignatorManager.Deselect();
        }
        
        public override AcceptanceReport CanDesignateCell(IntVec3 loc)
        {
            if (currentArea == null)
                return "没有选择区域";
            
            if (!loc.InBounds(Map))
                return false;
            
            return true;
        }
        
        public override void DesignateSingleCell(IntVec3 c)
        {
            if (currentArea == null) return;
            
            currentArea[c] = isAdding;
            
            // 通知数据变化
            var manager = ColonyAnnouncementManager.Instance;
            if (manager != null)
                manager.NotifyDataChanged();
        }
        
        public override void DesignateMultiCell(System.Collections.Generic.IEnumerable<IntVec3> cells)
        {
            if (currentArea == null) return;
            
            bool somethingSucceeded = false;
            
            foreach (var cell in cells)
            {
                if (CanDesignateCell(cell).Accepted)
                {
                    currentArea[cell] = isAdding;
                    somethingSucceeded = true;
                }
            }
            
            if (somethingSucceeded)
            {
                // 通知数据变化
                var manager = ColonyAnnouncementManager.Instance;
                if (manager != null)
                    manager.NotifyDataChanged();
                
                // 播放成功音效
                Finalize(true);
            }
        }
        
        public override void RenderHighlight(System.Collections.Generic.List<IntVec3> dragCells)
        {
            // 拖拽时的视觉预览
            DesignatorUtility.RenderHighlightOverSelectableCells(this, dragCells);
        }
        
        public override void SelectedUpdate()
        {
            base.SelectedUpdate();
            
            // 绘制区域高亮
            if (currentArea != null)
            {
                DrawAreaOverlay();
            }
            
            // 绘制地图边界
            GenDraw.DrawNoBuildEdgeLines();
            
            // 绘制鼠标悬停框
            GenUI.RenderMouseoverBracket();
        }
        
        /// <summary>
        /// 绘制区域高亮覆盖层
        /// </summary>
        private void DrawAreaOverlay()
        {
            if (currentArea == null || currentArea.Cells == null) return;
            
            var map = Find.CurrentMap;
            if (map == null) return;
            
            // 创建带颜色的材质
            Color overlayColor = currentArea.Color;
            overlayColor.a = 0.5f; // 半透明
            
            Material coloredMaterial = SolidColorMaterials.SimpleSolidColorMaterial(overlayColor);
            
            // 绘制所有区域格子
            foreach (var cell in currentArea.ActiveCells)
            {
                if (cell.InBounds(map))
                {
                    Vector3 drawPos = cell.ToVector3ShiftedWithAltitude(AltitudeLayer.MetaOverlays);
                    Graphics.DrawMesh(
                        MeshPool.plane10,
                        drawPos,
                        Quaternion.identity,
                        coloredMaterial,
                        0
                    );
                }
            }
        }
        
        public override void DrawMouseAttachments()
        {
            // 不使用 DrawMouseAttachment，避免 API 版本问题
            // 信息会在 SelectedUpdate 中通过其他方式显示
        }
        
        public override void ProcessInput(Event ev)
        {
            if (currentArea == null)
            {
                Messages.Message("请先选择一个区域", MessageTypeDefOf.RejectInput, false);
                return;
            }
            
            base.ProcessInput(ev);
        }
    }
}
