using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance.UI
{
    /// <summary>
    /// 殖民地中心点调整窗口
    /// </summary>
    public class ColonyCenterAdjustDialog : Window
    {
        private IntVec3 tempOffset;
        private static Material markerMaterial;
        
        public override Vector2 InitialSize => new Vector2(420f, 320f);
        
        public ColonyCenterAdjustDialog()
        {
            tempOffset = RimTalkHealthEnhanceMod.Settings.ColonyCenterOffset;
            
            this.doCloseButton = false;
            this.doCloseX = true;
            this.absorbInputAroundWindow = false;
            this.draggable = true;
            this.preventCameraMotion = false;
            
            // 创建标记材质
            if (markerMaterial == null)
            {
                markerMaterial = SolidColorMaterials.SimpleSolidColorMaterial(new Color(1f, 1f, 0f, 0.5f));
                markerMaterial.shader = ShaderDatabase.MetaOverlay;
            }
        }
        
        public override void DoWindowContents(Rect inRect)
        {
            var map = Find.CurrentMap;
            if (map == null)
            {
                this.Close();
                return;
            }
            
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // 标题
            Text.Font = GameFont.Medium;
            listing.Label("RTE_ColonyCenter_Title".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();
            
            // 自动中心
            IntVec3 autoCenter = LocationContextBuilder.GetAutoCenter(map);
            listing.Label("RTE_ColonyCenter_AutoCenter".Translate(autoCenter.x, autoCenter.z));
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            // X轴偏移（滑块 + 输入框）
            Rect xRow = listing.GetRect(30f);
            Widgets.Label(xRow.LeftPart(0.3f), "RTE_ColonyCenter_OffsetX".Translate(tempOffset.x));
            
            // 滑块
            Rect xSliderRect = new Rect(xRow.x + xRow.width * 0.3f, xRow.y, xRow.width * 0.55f, xRow.height);
            tempOffset.x = (int)Widgets.HorizontalSlider(
                xSliderRect, 
                tempOffset.x, 
                -100, 100, 
                true, 
                null
            );
            
            // 输入框
            Rect xInputRect = new Rect(xRow.xMax - xRow.width * 0.12f, xRow.y, xRow.width * 0.12f, xRow.height);
            string xBuffer = tempOffset.x.ToString();
            Widgets.TextFieldNumeric(xInputRect, ref tempOffset.x, ref xBuffer, -100, 100);
            
            // Z轴偏移（滑块 + 输入框）
            Rect zRow = listing.GetRect(30f);
            Widgets.Label(zRow.LeftPart(0.3f), "RTE_ColonyCenter_OffsetZ".Translate(tempOffset.z));
            
            // 滑块
            Rect zSliderRect = new Rect(zRow.x + zRow.width * 0.3f, zRow.y, zRow.width * 0.55f, zRow.height);
            tempOffset.z = (int)Widgets.HorizontalSlider(
                zSliderRect, 
                tempOffset.z, 
                -100, 100, 
                true, 
                null
            );
            
            // 输入框
            Rect zInputRect = new Rect(zRow.xMax - zRow.width * 0.12f, zRow.y, zRow.width * 0.12f, zRow.height);
            string zBuffer = tempOffset.z.ToString();
            Widgets.TextFieldNumeric(zInputRect, ref tempOffset.z, ref zBuffer, -100, 100);
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            // 最终中心
            IntVec3 finalCenter = autoCenter + tempOffset;
            GUI.color = Color.yellow;
            listing.Label("RTE_ColonyCenter_FinalCenter".Translate(finalCenter.x, finalCenter.z));
            GUI.color = Color.white;
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            // 按钮行
            Rect buttonRow = listing.GetRect(35f);
            float btnWidth = (buttonRow.width - 10f) / 3f;
            
            // 重置按钮
            if (Widgets.ButtonText(new Rect(buttonRow.x, buttonRow.y, btnWidth, 35f), "RTE_ColonyCenter_Reset".Translate()))
            {
                tempOffset = IntVec3.Zero;
            }
            
            // 应用按钮
            if (Widgets.ButtonText(new Rect(buttonRow.x + btnWidth + 5f, buttonRow.y, btnWidth, 35f), "RTE_ColonyCenter_Apply".Translate()))
            {
                ApplyOffset();
            }
            
            // 应用并关闭按钮
            if (Widgets.ButtonText(new Rect(buttonRow.x + btnWidth * 2 + 10f, buttonRow.y, btnWidth, 35f), "RTE_ColonyCenter_ApplyClose".Translate()))
            {
                ApplyOffset();
                this.Close();
            }
            
            listing.Gap();
            
            // 提示文字
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            listing.Label("RTE_ColonyCenter_Hint".Translate());
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            listing.End();
        }
        
        private void ApplyOffset()
        {
            RimTalkHealthEnhanceMod.Settings.ColonyCenterOffset = tempOffset;
            RimTalkHealthEnhanceMod.Settings.Write();
            LocationContextBuilder.OnMapChanged();
            Messages.Message("RTE_ColonyCenter_Updated".Translate(), MessageTypeDefOf.TaskCompletion, false);
        }
        
        public override void PostClose()
        {
            base.PostClose();
            // 关闭窗口时，关闭切换按钮
            ColonyCenterPlaySetting.ShowCenterMarker = false;
        }
        
        // 每帧绘制中心点标记
        public override void WindowUpdate()
        {
            base.WindowUpdate();
            DrawCenterMarker();
        }
        
        private void DrawCenterMarker()
        {
            var map = Find.CurrentMap;
            if (map == null) return;
            
            IntVec3 center = LocationContextBuilder.GetAutoCenter(map) + tempOffset;
            
            // 绘制黄色高亮边缘
            GenDraw.DrawFieldEdges(new List<IntVec3> { center }, Color.yellow);
            
            // 绘制一个醒目的圆圈标记
            Vector3 drawPos = center.ToVector3Shifted();
            drawPos.y = AltitudeLayer.MetaOverlays.AltitudeFor();
            
            Graphics.DrawMesh(
                MeshPool.plane10,
                Matrix4x4.TRS(drawPos, Quaternion.identity, new Vector3(3f, 1f, 3f)),
                markerMaterial,
                0
            );
        }
    }
}
