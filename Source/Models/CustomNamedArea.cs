using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance.Models
{
    /// <summary>
    /// 自定义命名区域 - 完全独立的 Area 系统，专门为 AI 对话服务
    /// 实现 ICellBoolGiver 接口以支持高性能的 CellBoolDrawer 渲染
    /// </summary>
    public class CustomNamedArea : IExposable, ICellBoolGiver
    {
        public string Id;
        public string Label;
        public BoolGrid Cells;
        public Color Color = Color.white;
        public bool IsActive = true;
        public bool IsConstructionArea = false;  // 是否为施工区域（用于位置显示后缀）
        
        private Map map;
        private int cachedCellCount = -1; // 缓存格子数量，-1表示需要重新计算
        private IntVec3 cachedCenter = IntVec3.Invalid; // 缓存中心点
        
        // CellBoolDrawer 用于高性能渲染
        private CellBoolDrawer drawer;
        
        public Map Map => map;
        public int MapID => map?.uniqueID ?? -1;
        public bool IsEnabled => IsActive;
        
        /// <summary>
        /// ICellBoolGiver 接口实现 - 返回区域颜色
        /// </summary>
        Color ICellBoolGiver.Color => Color;
        
        /// <summary>
        /// 获取 CellBoolDrawer（懒加载）
        /// </summary>
        private CellBoolDrawer Drawer
        {
            get
            {
                if (drawer == null && map != null)
                {
                    drawer = new CellBoolDrawer(this, map.Size.x, map.Size.z, 3650, 0.33f);
                }
                return drawer;
            }
        }
        
        /// <summary>
        /// 计算区域中心点（带缓存）
        /// </summary>
        public IntVec3 Center
        {
            get
            {
                if (cachedCenter != IntVec3.Invalid) return cachedCenter;
                
                if (Cells == null || map == null) return IntVec3.Invalid;
                
                long sumX = 0, sumZ = 0;
                int count = 0;
                
                foreach (var cell in ActiveCells)
                {
                    sumX += cell.x;
                    sumZ += cell.z;
                    count++;
                }
                
                if (count == 0) return IntVec3.Invalid;
                cachedCenter = new IntVec3((int)(sumX / count), 0, (int)(sumZ / count));
                return cachedCenter;
            }
        }
        
        // 无参构造函数（用于序列化）
        public CustomNamedArea()
        {
        }
        
        public CustomNamedArea(Map map, string label)
        {
            this.map = map;
            this.Id = Guid.NewGuid().ToString();
            this.Label = label;
            this.Cells = new BoolGrid(map);
            this.Color = GetRandomColor();
        }
        
        /// <summary>
        /// ICellBoolGiver 接口实现 - 检查某个格子是否激活
        /// </summary>
        public bool GetCellBool(int index)
        {
            if (Cells == null || map == null) return false;
            return Cells[index];
        }
        
        /// <summary>
        /// ICellBoolGiver 接口实现 - 获取额外颜色（返回白色表示使用默认颜色）
        /// </summary>
        public Color GetCellExtraColor(int index)
        {
            return UnityEngine.Color.white;
        }
        
        /// <summary>
        /// 检测某个位置是否在区域内
        /// </summary>
        public bool this[IntVec3 c]
        {
            get => Cells != null && Cells[c];
            set
            {
                if (Cells != null)
                {
                    Cells[c] = value;
                    InvalidateCache(); // 修改时使缓存失效
                    MarkDrawerDirty(); // 标记绘制器需要更新
                }
            }
        }
        
        /// <summary>
        /// 获取区域内的所有格子
        /// </summary>
        public IEnumerable<IntVec3> ActiveCells
        {
            get
            {
                if (Cells == null || map == null) yield break;
                
                foreach (var cell in map.AllCells)
                {
                    if (Cells[cell])
                        yield return cell;
                }
            }
        }
        
        /// <summary>
        /// 获取区域内的格子数量（带缓存，性能优化）
        /// </summary>
        public int CellCount
        {
            get
            {
                if (cachedCellCount >= 0) return cachedCellCount;
                
                if (Cells == null) return 0;
                
                int count = 0;
                foreach (var cell in ActiveCells)
                    count++;
                
                cachedCellCount = count;
                return count;
            }
        }
        
        /// <summary>
        /// 使缓存失效（在修改区域时调用）
        /// </summary>
        private void InvalidateCache()
        {
            cachedCellCount = -1;
            cachedCenter = IntVec3.Invalid;
        }
        
        /// <summary>
        /// 标记绘制器需要重新生成网格
        /// </summary>
        private void MarkDrawerDirty()
        {
            drawer?.SetDirty();
        }
        
        /// <summary>
        /// 标记需要绘制
        /// </summary>
        public void MarkForDraw()
        {
            if (map == Find.CurrentMap && !Find.ScreenshotModeHandler.Active)
            {
                Drawer?.MarkForDraw();
            }
        }
        
        /// <summary>
        /// 执行绘制更新（每帧调用）
        /// </summary>
        public void AreaUpdate()
        {
            Drawer?.CellBoolDrawerUpdate();
        }
        
        /// <summary>
        /// 清空区域
        /// </summary>
        public void Clear()
        {
            if (Cells != null && map != null)
            {
                foreach (var cell in map.AllCells)
                    Cells[cell] = false;
                InvalidateCache();
                MarkDrawerDirty();
            }
        }
        
        /// <summary>
        /// 设置整个矩形区域
        /// </summary>
        public void SetRect(CellRect rect, bool value)
        {
            if (Cells == null || map == null) return;
            
            foreach (var cell in rect)
            {
                if (cell.InBounds(map))
                    Cells[cell] = value;
            }
            InvalidateCache();
            MarkDrawerDirty();
        }
        
        /// <summary>
        /// 重新关联地图（加载存档后使用）
        /// </summary>
        public void ReassignMap(Map newMap)
        {
            this.map = newMap;
            
            // 重置绘制器（地图变化后需要重新创建）
            drawer = null;
            
            // 如果 BoolGrid 为空，重新创建
            if (Cells == null && newMap != null)
                Cells = new BoolGrid(newMap);
            
            InvalidateCache(); // 重新关联地图后使缓存失效
        }
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Id, "id");
            Scribe_Values.Look(ref Label, "label");
            Scribe_Values.Look(ref Color, "color", Color.white);
            Scribe_Values.Look(ref IsActive, "isActive", true);
            Scribe_Values.Look(ref IsConstructionArea, "isConstructionArea", false);
            Scribe_Deep.Look(ref Cells, "cells");
        }
        
        /// <summary>
        /// 生成随机颜色（用于新区域）
        /// </summary>
        private static Color GetRandomColor()
        {
            var colors = new[]
            {
                new Color(0.2f, 0.8f, 0.2f),  // 绿色
                new Color(0.2f, 0.5f, 0.9f),  // 蓝色
                new Color(0.9f, 0.7f, 0.2f),  // 黄色
                new Color(0.9f, 0.3f, 0.3f),  // 红色
                new Color(0.7f, 0.3f, 0.9f),  // 紫色
                new Color(0.3f, 0.9f, 0.9f),  // 青色
                new Color(0.9f, 0.5f, 0.2f),  // 橙色
                new Color(0.9f, 0.3f, 0.7f),  // 粉色
            };
            
            return colors[UnityEngine.Random.Range(0, colors.Length)];
        }
    }
}
