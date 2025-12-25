using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using HarmonyLib;
using System.Reflection;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 全局地图布局构建服务 - 提供整个殖民地的空间结构信息
    /// </summary>
    public static class ColonyLayoutBuilder
    {
        private class ColonyLayoutCache
        {
            public string CachedText;
            public int LastUpdateTick;
            public bool IsDirty = true;
        }

        private static Dictionary<int, ColonyLayoutCache> _mapCaches = new Dictionary<int, ColonyLayoutCache>();

        /// <summary>
        /// 标记指定地图的缓存失效
        /// </summary>
        public static void InvalidateCache(Map map)
        {
            if (map == null) return;
            
            if (!_mapCaches.TryGetValue(map.uniqueID, out var cache))
            {
                cache = new ColonyLayoutCache();
                _mapCaches[map.uniqueID] = cache;
            }
            
            cache.IsDirty = true;
        }

        /// <summary>
        /// 获取殖民地布局文本
        /// </summary>
        public static string GetColonyLayout(Map map)
        {
            if (map == null || !map.IsPlayerHome) return null;

            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.EnableGlobalLayout) return null;

            if (!_mapCaches.TryGetValue(map.uniqueID, out var cache))
            {
                cache = new ColonyLayoutCache();
                _mapCaches[map.uniqueID] = cache;
            }

            // 如果缓存有效且未过期（防止每帧更新，虽然 IsDirty 应该控制得很好）
            if (!cache.IsDirty)
            {
                return cache.CachedText;
            }

            // 重新生成
            cache.CachedText = GenerateLayoutText(map, settings);
            cache.IsDirty = false;
            cache.LastUpdateTick = Find.TickManager.TicksGame;

            return cache.CachedText;
        }

        private static string GenerateLayoutText(Map map, HealthEnhanceSettings settings)
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Colony Layout ===");

            // 计算殖民地中心
            IntVec3 center = CalculateHomeAreaCenter(map);

            // 收集所有房间和区域
            var items = new List<LayoutItem>();

            // 1. 收集房间
            List<Room> allRooms = null;
            try
            {
                // 使用 AccessTools 获取 allRooms (参考 SnapshotService)
                var field = AccessTools.Field(typeof(RegionGrid), "allRooms");
                if (field != null)
                {
                    allRooms = field.GetValue(map.regionGrid) as List<Room>;
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[RimTalk Enhance] Failed to get allRooms via reflection: {ex.Message}");
            }

            if (allRooms == null)
            {
                // 备用方案：遍历所有 Region
                allRooms = new List<Room>();
                var visitedRooms = new HashSet<Room>();
                
                try 
                {
                    // 尝试获取 AllRegions 属性
                    var prop = AccessTools.Property(typeof(RegionGrid), "AllRegions");
                    if (prop != null)
                    {
                        var regions = prop.GetValue(map.regionGrid, null) as IEnumerable<Region>;
                        if (regions != null)
                        {
                            foreach (var region in regions)
                            {
                                if (region.Room != null && !visitedRooms.Contains(region.Room))
                                {
                                    visitedRooms.Add(region.Room);
                                    allRooms.Add(region.Room);
                                }
                            }
                        }
                    }
                }
                catch { }
            }

            if (allRooms != null)
            {
                foreach (var room in allRooms)
                {
                    if (room.PsychologicallyOutdoors || room.Role == RoomRoleDefOf.None) continue;
                    
                    // 过滤：面积
                    if (room.CellCount < settings.MinRoomSize) continue;

                    // 计算中心点
                    IntVec3 roomCenter = IntVec3.Invalid;
                    if (room.Cells.Any())
                    {
                        // 简单计算平均值
                        long sumX = 0, sumZ = 0;
                        int count = 0;
                        foreach (var c in room.Cells)
                        {
                            sumX += c.x;
                            sumZ += c.z;
                            count++;
                        }
                        roomCenter = new IntVec3((int)(sumX / count), 0, (int)(sumZ / count));
                    }
                    
                    if (!roomCenter.IsValid) continue;

                    // 过滤：距离
                    if (settings.MaxLayoutDistance > 0 && roomCenter.DistanceTo(center) > settings.MaxLayoutDistance) continue;

                    // 过滤：名称
                    string name = GetRoomName(room);
                    if (settings.OnlyShowNamedRooms)
                    {
                        // 过滤通用名称（支持多语言）
                        if (name == "Room" || name == "None" || name == "房间" || name == "无")
                            continue;
                    }

                    // 去重：如果房间中心位于某个自定义区域内，跳过该房间（优先显示自定义区域）
                    if (settings.IncludeCustomAreas && IsInCustomArea(roomCenter))
                        continue;

                    items.Add(new LayoutItem
                    {
                        Name = name,
                        Type = "Room",
                        Center = roomCenter,
                        Size = room.CellCount,
                        Direction = GetRelativeDirection(roomCenter, center),
                        Distance = (int)roomCenter.DistanceTo(center)
                    });
                }
            }

            // 2. 收集自定义区域
            if (settings.IncludeCustomAreas)
            {
                var manager = ColonyAnnouncementManager.Instance;
                if (manager != null)
                {
                    foreach (var area in manager.CustomAreas)
                    {
                        if (area.MapID != map.uniqueID || !area.IsEnabled) continue;
                        if (area.CellCount == 0) continue;

                        IntVec3 areaCenter = area.Center;
                        
                        // 过滤：距离
                        if (settings.MaxLayoutDistance > 0 && areaCenter.DistanceTo(center) > settings.MaxLayoutDistance) continue;

                        items.Add(new LayoutItem
                        {
                            Name = area.Label,
                            Type = "Area",
                            Center = areaCenter,
                            Size = area.CellCount,
                            Direction = GetRelativeDirection(areaCenter, center),
                            Distance = (int)areaCenter.DistanceTo(center)
                        });
                    }
                }
            }

            // 3. 收集原生区域 (Growing/Storage) - 可选，为了避免太杂乱，暂时只收集重要的
            // 如果需要可以添加

            if (items.Count == 0) return null;

            // 分组输出
            if (settings.GroupByDirection)
            {
                var grouped = items.GroupBy(x => GetDirectionGroup(x.Direction))
                                   .OrderBy(g => GetDirectionOrder(g.Key));

                foreach (var group in grouped)
                {
                    sb.AppendLine($"{group.Key} Zone ({group.Count()} areas):");
                    foreach (var item in group.OrderBy(x => x.Distance))
                    {
                        sb.AppendLine($"  - {item.Name} ({item.Size} cells, {item.Distance} cells {item.Direction.ToLower()} of center)");
                    }
                    sb.AppendLine();
                }
            }
            else
            {
                // 按类型分组 (Room vs Area)
                var grouped = items.GroupBy(x => x.Type).OrderBy(g => g.Key);
                foreach (var group in grouped)
                {
                    sb.AppendLine($"{group.Key}s:");
                    foreach (var item in group.OrderBy(x => x.Distance))
                    {
                        sb.AppendLine($"  - {item.Name} ({item.Size} cells, {item.Direction} of center)");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString().TrimEnd();
        }

        private class LayoutItem
        {
            public string Name;
            public string Type;
            public IntVec3 Center;
            public int Size;
            public string Direction;
            public int Distance;
        }

        // 复用 LocationContextBuilder 的逻辑
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
                if (count > 5000 && count % 10 != 0) continue;
            }

            if (count == 0) return map.Center;
            return new IntVec3((int)(sumX / count), 0, (int)(sumZ / count));
        }

        private static string GetRelativeDirection(IntVec3 position, IntVec3 center)
        {
            int dx = position.x - center.x;
            int dz = position.z - center.z;

            if (Mathf.Abs(dx) < 5 && Mathf.Abs(dz) < 5) return "Center";

            if (Mathf.Abs(dx) > Mathf.Abs(dz) * 2) return dx > 0 ? "East" : "West";
            else if (Mathf.Abs(dz) > Mathf.Abs(dx) * 2) return dz > 0 ? "North" : "South";
            else if (dx > 0 && dz > 0) return "Northeast";
            else if (dx > 0 && dz < 0) return "Southeast";
            else if (dx < 0 && dz > 0) return "Northwest";
            else return "Southwest";
        }

        private static string GetDirectionGroup(string direction)
        {
            if (direction == "Center") return "Central";
            if (direction.Contains("North")) return "North";
            if (direction.Contains("South")) return "South";
            if (direction.Contains("East")) return "East";
            if (direction.Contains("West")) return "West";
            return "Other";
        }

        private static int GetDirectionOrder(string group)
        {
            switch (group)
            {
                case "Central": return 0;
                case "North": return 1;
                case "East": return 2;
                case "South": return 3;
                case "West": return 4;
                default: return 5;
            }
        }

        private static string GetRoomName(Room room)
        {
            if (room == null) return "Room";
            try
            {
                // 优先使用 GetRoomRoleLabel()，它会返回带主人名字的完整标签（如"yaowen的卧室"）
                var method = room.GetType().GetMethod("GetRoomRoleLabel");
                if (method != null)
                {
                    string roleLabel = method.Invoke(room, null) as string;
                    if (!string.IsNullOrEmpty(roleLabel))
                        return roleLabel;
                }
                
                // 备用方案：使用 LabelCap
                var labelCapProp = room.GetType().GetProperty("LabelCap");
                if (labelCapProp != null)
                {
                    string labelCap = labelCapProp.GetValue(room) as string;
                    if (!string.IsNullOrEmpty(labelCap))
                        return labelCap;
                }
            }
            catch { }
            return room.Role?.label ?? "Room";
        }

        private static bool IsInCustomArea(IntVec3 cell)
        {
            var manager = ColonyAnnouncementManager.Instance;
            if (manager != null)
            {
                var area = manager.GetCustomAreaAt(cell);
                return area != null && area.IsEnabled;
            }
            return false;
        }
    }
}
