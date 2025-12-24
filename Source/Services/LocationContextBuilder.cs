using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 位置上下文构建服务 - 提供 Pawn 相对于殖民地的位置信息
    /// </summary>
    public static class LocationContextBuilder
    {
        // 缓存殖民地中心点
        private static IntVec3 _homeAreaCenter = IntVec3.Invalid;
        private static int _lastUpdateTick = 0;
        private const int UPDATE_INTERVAL = 420; // 7秒 = 420 ticks (跟随 RimTalk 的 TalkInterval)

        /// <summary>
        /// 获取 Pawn 的相对位置信息
        /// </summary>
        public static string GetRelativeLocation(Pawn pawn)
        {
            if (pawn?.Map == null) return null;

            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.ShowRelativeLocation) return null;

            // 如果不在主基地，只显示地图名称和派系信息
            if (!pawn.Map.IsPlayerHome)
            {
                return GetNonHomeMapInfo(pawn.Map);
            }

            // 在主基地，显示完整的相对位置信息
            // 更新殖民地中心（定期）
            UpdateHomeAreaCenterIfNeeded(pawn.Map);

            IntVec3 position = pawn.Position;
            var room = pawn.GetRoom();

            // 判断城镇区域
            string zone = GetTownZone(pawn, position);

            // 判断方位
            string direction = GetRelativeDirection(position, _homeAreaCenter);

            // 检测 Area 信息
            string areaInfo = null;
            if (settings.ShowAreaInfo)
            {
                areaInfo = GetAreaInfo(pawn, position);
            }

            // 检测附近房间（仅室外）
            string nearbyRoom = null;
            if (room == null || room.PsychologicallyOutdoors)
            {
                nearbyRoom = GetNearbyRoomInfo(pawn, position);
            }

            // 构建位置字符串
            return BuildLocationString(nearbyRoom, direction, zone, areaInfo);
        }

        /// <summary>
        /// 获取非主基地地图的基本信息（地图名称和派系）
        /// </summary>
        private static string GetNonHomeMapInfo(Map map)
        {
            var mapParent = map.Parent;
            if (mapParent == null) return null;

            var parts = new List<string>();

            // 地图名称
            parts.Add($"at {mapParent.Label}");

            // 派系信息（如果有且非玩家派系）
            if (mapParent.Faction != null && !mapParent.Faction.IsPlayer)
            {
                parts.Add($"({mapParent.Faction.Name} territory)");
            }

            return string.Join(" ", parts);
        }

        /// <summary>
        /// 构建位置描述字符串
        /// </summary>
        private static string BuildLocationString(string nearbyRoom, string direction, string zone, string areaInfo)
        {
            string result = "";

            // 添加附近房间信息（仅室外）
            if (!string.IsNullOrEmpty(nearbyRoom))
            {
                result = nearbyRoom;
            }

            // 添加 Area 信息
            if (!string.IsNullOrEmpty(areaInfo))
            {
                if (!string.IsNullOrEmpty(result))
                    result += ", ";
                result += areaInfo;
            }

            // 添加方位
            if (!string.IsNullOrEmpty(direction))
            {
                if (!string.IsNullOrEmpty(result))
                    result += ", ";
                result += $"{direction} of colony";
            }

            // 添加区域类型
            if (!string.IsNullOrEmpty(zone))
            {
                if (!string.IsNullOrEmpty(result))
                    result += " ";
                result += $"({zone})";
            }

            return result;
        }

        /// <summary>
        /// 获取附近房间信息（仅室外使用，扫描周围3格）
        /// </summary>
        private static string GetNearbyRoomInfo(Pawn pawn, IntVec3 position)
        {
            var map = pawn.Map;

            // 优化：先扫描相邻8格（最可能找到房间）
            foreach (var offset in GenAdj.AdjacentCells)
            {
                IntVec3 cell = position + offset;
                if (!cell.InBounds(map)) continue;

                var room = cell.GetRoom(map);
                if (room != null && !room.PsychologicallyOutdoors && room.Role != null)
                {
                    string roomName = GetRoomName(room);
                    return $"near {roomName}";
                }
            }

            // 如果相邻格没找到，再扫描外围（距离2-3格）
            const int RADIUS = 3;
            for (int dx = -RADIUS; dx <= RADIUS; dx++)
            {
                for (int dz = -RADIUS; dz <= RADIUS; dz++)
                {
                    // 跳过已经扫描过的相邻8格和中心点
                    if (Math.Abs(dx) <= 1 && Math.Abs(dz) <= 1) continue;

                    IntVec3 cell = position + new IntVec3(dx, 0, dz);
                    if (!cell.InBounds(map)) continue;

                    var room = cell.GetRoom(map);
                    if (room != null && !room.PsychologicallyOutdoors && room.Role != null)
                    {
                        string roomName = GetRoomName(room);
                        return $"near {roomName}";
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 获取房间名称（优先自定义名称）
        /// </summary>
        private static string GetRoomName(Room room)
        {
            if (room == null) return "Room";

            // 尝试获取自定义标签
            try
            {
                var labelCapProp = room.GetType().GetProperty("LabelCap");
                if (labelCapProp != null)
                {
                    string labelCap = labelCapProp.GetValue(room) as string;
                    if (!string.IsNullOrEmpty(labelCap) && labelCap != room.Role?.label)
                    {
                        return labelCap;
                    }
                }
            }
            catch
            {
                // 忽略反射错误
            }

            // 回退到角色标签
            return room.Role?.label ?? "Room";
        }

        /// <summary>
        /// 获取 Area 信息（种植区、储存区、自定义区域等）
        /// </summary>
        private static string GetAreaInfo(Pawn pawn, IntVec3 position)
        {
            var map = pawn.Map;

            // 1. 检测自定义命名区域（优先级最高）
            var manager = ColonyAnnouncementManager.Instance;
            if (manager != null)
            {
                var customArea = manager.GetCustomAreaAt(position);
                if (customArea != null)
                {
                    // 施工区域添加后缀
                    if (customArea.IsConstructionArea)
                        return $"in {customArea.Label} (施工区域)";
                    else
                        return $"in {customArea.Label}";
                }
            }

            // 2. 检测原版功能性区域
            // 种植区
            foreach (var zone in map.zoneManager.AllZones)
            {
                if (zone is Zone_Growing growZone && growZone.ContainsCell(position))
                {
                    string label = !string.IsNullOrEmpty(growZone.label) ? growZone.label : "Growing Zone";
                    return $"in {label}";
                }
            }

            // 储存区
            foreach (var zone in map.zoneManager.AllZones)
            {
                if (zone is Zone_Stockpile stockpile && stockpile.ContainsCell(position))
                {
                    string label = !string.IsNullOrEmpty(stockpile.label) ? stockpile.label : "Storage Zone";
                    return $"in {label}";
                }
            }

            return null;
        }

        /// <summary>
        /// 判断城镇区域类型
        /// </summary>
        private static string GetTownZone(Pawn pawn, IntVec3 position)
        {
            var homeArea = pawn.Map.areaManager.Home;

            // 不在居住区 = 野外
            if (homeArea == null || !homeArea[position])
                return "Wilderness";

            var settings = RimTalkHealthEnhanceMod.Settings;

            // 检测周围8格有多少在居住区内
            int neighborCount = 0;
            foreach (var offset in GenAdj.AdjacentCells)
            {
                IntVec3 neighbor = position + offset;
                if (neighbor.InBounds(pawn.Map) && homeArea[neighbor])
                    neighborCount++;
            }

            // 8格都在居住区内
            if (neighborCount == 8)
            {
                // 可选：检测是否是核心区
                if (settings.EnableTownCenterDetection)
                {
                    float distanceToCenter = position.DistanceTo(_homeAreaCenter);
                    if (distanceToCenter <= settings.TownCenterRadius)
                        return "Town Center";
                }
                return "Town";
            }

            // 5-7格在居住区内 = 城镇内部
            if (neighborCount >= 5)
                return "Town";

            // 1-4格在居住区内 = 城镇边缘
            return "Town Edge";
        }

        /// <summary>
        /// 获取相对方位（8方位）
        /// </summary>
        private static string GetRelativeDirection(IntVec3 position, IntVec3 center)
        {
            if (!center.IsValid) return "";

            int dx = position.x - center.x;
            int dz = position.z - center.z;

            // 非常接近中心（5格内）
            if (Mathf.Abs(dx) < 5 && Mathf.Abs(dz) < 5)
                return "Center";

            // 8方位判断
            if (Mathf.Abs(dx) > Mathf.Abs(dz) * 2)
                return dx > 0 ? "East" : "West";
            else if (Mathf.Abs(dz) > Mathf.Abs(dx) * 2)
                return dz > 0 ? "North" : "South";
            else if (dx > 0 && dz > 0) return "Northeast";
            else if (dx > 0 && dz < 0) return "Southeast";
            else if (dx < 0 && dz > 0) return "Northwest";
            else return "Southwest";
        }

        /// <summary>
        /// 定期更新殖民地中心点
        /// </summary>
        private static void UpdateHomeAreaCenterIfNeeded(Map map)
        {
            int currentTick = Find.TickManager.TicksGame;

            if (currentTick - _lastUpdateTick < UPDATE_INTERVAL)
                return;

            _homeAreaCenter = CalculateHomeAreaCenter(map);
            _lastUpdateTick = currentTick;
        }

        /// <summary>
        /// 计算殖民地中心（基于居住区）
        /// </summary>
        private static IntVec3 CalculateHomeAreaCenter(Map map)
        {
            var homeArea = map.areaManager.Home;
            if (homeArea == null || homeArea.TrueCount == 0)
                return map.Center;

            long sumX = 0, sumZ = 0;
            int count = 0;

            foreach (var cell in homeArea.ActiveCells)
            {
                sumX += cell.x;
                sumZ += cell.z;
                count++;

                // 性能优化：大型居住区采样计算
                if (count > 5000 && count % 10 != 0)
                    continue;
            }

            if (count == 0) return map.Center;

            return new IntVec3((int)(sumX / count), 0, (int)(sumZ / count));
        }

        /// <summary>
        /// 地图切换时清空缓存
        /// </summary>
        public static void OnMapChanged()
        {
            _homeAreaCenter = IntVec3.Invalid;
            _lastUpdateTick = 0;
        }
    }
}
