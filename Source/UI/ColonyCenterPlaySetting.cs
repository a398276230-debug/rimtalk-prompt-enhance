using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance.UI
{
    /// <summary>
    /// 殖民地中心点 PlaySettings 切换按钮
    /// </summary>
    public static class ColonyCenterPlaySetting
    {
        private static bool _showCenterMarker = false;
        
        public static bool ShowCenterMarker
        {
            get => _showCenterMarker;
            set
            {
                if (_showCenterMarker != value)
                {
                    _showCenterMarker = value;
                    
                    if (value)
                    {
                        // 打开调整窗口
                        Find.WindowStack.Add(new ColonyCenterAdjustDialog());
                    }
                }
            }
        }
        
        /// <summary>
        /// 绘制 PlaySettings 按钮
        /// </summary>
        public static void DoPlaySettingsGlobalControls(WidgetRow row, bool worldView)
        {
            if (worldView) return; // 世界地图不显示
            
            var map = Find.CurrentMap;
            if (map == null || !map.IsPlayerHome) return;
            
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowRelativeLocation) return;
            
            // 加载图标
            Texture2D icon = ContentFinder<Texture2D>.Get("UI/Buttons/ColonyCenterGizmo", true);
            
            // 保存旧值
            bool oldValue = _showCenterMarker;
            
            // 绘制切换按钮
            row.ToggleableIcon(
                ref _showCenterMarker,
                icon,
                "RTE_ColonyCenter_Tooltip".Translate(),
                SoundDefOf.Mouseover_ButtonToggle
            );
            
            // 检测状态变化
            if (_showCenterMarker != oldValue)
            {
                if (_showCenterMarker)
                {
                    // 打开调整窗口
                    Find.WindowStack.Add(new ColonyCenterAdjustDialog());
                }
                else
                {
                    // 关闭调整窗口
                    foreach (var window in Find.WindowStack.Windows)
                    {
                        if (window is ColonyCenterAdjustDialog dialog)
                        {
                            Find.WindowStack.TryRemove(dialog, false);
                            break;
                        }
                    }
                }
            }
        }
    }
}
