using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 位置上下文相关设置
    /// </summary>
    public class LocationSettings : IExposable
    {
        // === 基础位置设置 ===
        public bool ShowRelativeLocation = true;         // 启用相对位置显示
        public bool ShowAreaInfo = true;                 // 显示 Area 信息
        public bool EnableTownCenterDetection = false;   // 启用城镇核心检测
        public int TownCenterRadius = 20;                // 城镇核心半径
        public IntVec3 ColonyCenterOffset = IntVec3.Zero; // 殖民地中心点偏移
        
        // === 全局布局设置 ===
        public bool EnableGlobalLayout = false;          // 启用全局布局信息
        public int MinRoomSize = 9;                      // 最小房间面积
        public int MaxLayoutDistance = 100;              // 最大距离
        public bool IncludeCustomAreas = true;           // 包含自定义区域
        public bool GroupByDirection = true;             // 按方位分组
        public bool OnlyShowNamedRooms = true;           // 只显示有名称的房间

        public void ExposeData()
        {
            Scribe_Values.Look(ref ShowRelativeLocation, "showRelativeLocation", true);
            Scribe_Values.Look(ref ShowAreaInfo, "showAreaInfo", true);
            Scribe_Values.Look(ref EnableTownCenterDetection, "enableTownCenterDetection", false);
            Scribe_Values.Look(ref TownCenterRadius, "townCenterRadius", 20);
            Scribe_Values.Look(ref ColonyCenterOffset, "colonyCenterOffset", IntVec3.Zero);
            
            Scribe_Values.Look(ref EnableGlobalLayout, "enableGlobalLayout", false);
            Scribe_Values.Look(ref MinRoomSize, "minRoomSize", 9);
            Scribe_Values.Look(ref MaxLayoutDistance, "maxLayoutDistance", 100);
            Scribe_Values.Look(ref IncludeCustomAreas, "includeCustomAreas", true);
            Scribe_Values.Look(ref GroupByDirection, "groupByDirection", true);
            Scribe_Values.Look(ref OnlyShowNamedRooms, "onlyShowNamedRooms", true);
        }

        /// <summary>
        /// 绘制位置设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Location_Enable".Translate(), ref ShowRelativeLocation, 
                "RTE_Settings_Location_Enable_Desc".Translate());

            if (ShowRelativeLocation)
            {
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Location_ShowArea".Translate(), ref ShowAreaInfo,
                    "RTE_Settings_Location_ShowArea_Desc".Translate());
                
                listing.Gap();
                
                listing.CheckboxLabeled("RTE_Settings_Location_TownCenter".Translate(), ref EnableTownCenterDetection,
                    "RTE_Settings_Location_TownCenter_Desc".Translate());
                
                if (EnableTownCenterDetection)
                {
                    Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_CenterRadius".Translate(TownCenterRadius));
                    TownCenterRadius = (int)listing.Slider(TownCenterRadius, 10, 50);
                    
                    Text.Font = GameFont.Tiny;
                    GUI.color = Color.gray;
                    Widgets.Label(listing.GetRect(18f), "RTE_Settings_Location_CenterRadius_Desc".Translate(TownCenterRadius));
                    GUI.color = Color.white;
                    Text.Font = GameFont.Small;
                }
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(100f), 
                "说明：\n" +
                "1. 系统会自动计算殖民地中心（基于居住区）。\n" +
                "2. 提供8方位判断（东、南、西、北及四个斜向）。\n" +
                "3. 区域类型：Town Center（核心）、Town（城镇）、Town Edge（边缘）、Wilderness（野外）。\n" +
                "4. 自动检测种植区、储存区等游戏原生区域。\n" +
                "5. 信息会自动注入到 AI 的上下文中，让 AI 了解 Pawn 的位置。");
            Text.Font = GameFont.Small;

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            listing.CheckboxLabeled("RTE_Settings_Location_EnableGlobalLayout".Translate(), ref EnableGlobalLayout,
                "RTE_Settings_Location_EnableGlobalLayout_Desc".Translate());

            if (EnableGlobalLayout)
            {
                listing.Gap();
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_MinRoomSize".Translate(MinRoomSize));
                MinRoomSize = (int)listing.Slider(MinRoomSize, 4, 50);
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Location_MaxDistance".Translate(MaxLayoutDistance));
                MaxLayoutDistance = (int)listing.Slider(MaxLayoutDistance, 0, 300);
                
                listing.CheckboxLabeled("RTE_Settings_Location_OnlyNamedRooms".Translate(), ref OnlyShowNamedRooms,
                    "RTE_Settings_Location_OnlyNamedRooms_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Location_IncludeCustomAreas".Translate(), ref IncludeCustomAreas,
                    "RTE_Settings_Location_IncludeCustomAreas_Desc".Translate());
                
                listing.CheckboxLabeled("RTE_Settings_Location_GroupByDirection".Translate(), ref GroupByDirection,
                    "RTE_Settings_Location_GroupByDirection_Desc".Translate());
                
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(1f, 0.8f, 0.6f);
                Widgets.Label(listing.GetRect(40f), "RTE_Settings_Location_GlobalLayout_Warning".Translate());
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "RTE_Settings_Location_ExampleTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 1f, 0.8f);
            Widgets.Label(listing.GetRect(20f), "• In Bedroom, Northeast of colony (Town)");
            Widgets.Label(listing.GetRect(20f), "• Outdoors in Growing Zone, South of colony (Town Edge)");
            Widgets.Label(listing.GetRect(20f), "• Outdoors, North of colony (Wilderness)");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
        }
    }
}