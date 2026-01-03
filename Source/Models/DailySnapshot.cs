using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public class DailySnapshot : IExposable
    {
        // 绝对时间戳 - 唯一标识，用于排序和过滤
        public long AbsTick;
        
        // 兼容旧版本的字段（已弃用，仅用于加载旧存档）
        [System.Obsolete("Use AbsTick instead")]
        public int Day;
        [System.Obsolete("Use AbsTick instead")]
        public int Tick;
        
        // AI 生成的总结
        public string AISummary = "";
        
        // 结构化数据
        public ColonySnapshot Snapshot;
        public List<string> PlayerActions = new List<string>();  // 玩家操作日志
        public List<string> Events = new List<string>();  // 当日事件
        
        // 差分报告（原始文本）
        public string DiffReport = "";
        
        // 用于标记此快照是否有效
        public bool IsValid => AbsTick > 0;
        
        /// <summary>
        /// 获取游戏内第几天（从0开始，用于显示"第X天"）
        /// </summary>
        public int GameDay => AbsTick > 0 ? GenDate.DaysPassedAt(GenDate.TickAbsToGame((int)AbsTick)) : 0;
        
        /// <summary>
        /// 获取显示用的日期字符串（如"5500年赫象第6天"）
        /// </summary>
        public string GetDateString(Vector2 location)
        {
            if (AbsTick <= 0) return "(Invalid Date)";
            return GenDate.DateFullStringAt(AbsTick, location);
        }
        
        /// <summary>
        /// 获取显示用的日期字符串（带偏移量）
        /// </summary>
        public string GetDateStringWithOffset(long tickOffset, Vector2 location)
        {
            if (AbsTick <= 0) return "(Invalid Date)";
            return GenDate.DateFullStringAt(AbsTick + tickOffset, location);
        }
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref AbsTick, "absTick", 0L);
            
            // 兼容旧版本：读取旧字段
            #pragma warning disable CS0612 // 禁用弃用警告
            Scribe_Values.Look(ref Day, "day", 0);
            Scribe_Values.Look(ref Tick, "tick", 0);
            #pragma warning restore CS0612
            
            // 如果是加载旧存档，从旧字段转换到 AbsTick
            if (Scribe.mode == LoadSaveMode.PostLoadInit && AbsTick == 0)
            {
                #pragma warning disable CS0612
                // 尝试从 Tick 恢复（如果 Tick > 0）
                if (Tick > 0)
                {
                    try
                    {
                        AbsTick = GenDate.TickGameToAbs(Tick);
                        Log.Message($"[RimTalk Enhance] Migrated DailySnapshot from Tick={Tick} to AbsTick={AbsTick}");
                    }
                    catch (System.Exception ex)
                    {
                        Log.Warning($"[RimTalk Enhance] Failed to migrate Tick to AbsTick: {ex.Message}. Using fallback.");
                        // 备用方案：使用 Day 估算
                        if (Day > 0)
                        {
                            AbsTick = GenDate.TickGameToAbs(Day * GenDate.TicksPerDay);
                        }
                    }
                }
                // 如果 Tick 为 0，尝试从 Day 恢复
                else if (Day > 0)
                {
                    // Day 是游戏天数，转换为 AbsTick
                    AbsTick = GenDate.TickGameToAbs(Day * GenDate.TicksPerDay);
                    Log.Message($"[RimTalk Enhance] Migrated DailySnapshot from Day={Day} to AbsTick={AbsTick}");
                }
                // 都无法恢复时，生成一个基于当前时间的估算值（避免 AbsTick=0 的问题）
                else
                {
                    // 标记为无效，稍后会被清除
                    Log.Warning($"[RimTalk Enhance] DailySnapshot has no valid time data (Day={Day}, Tick={Tick}). Marking as invalid.");
                }
                #pragma warning restore CS0612
            }
            
            Scribe_Values.Look(ref AISummary, "summary");
            Scribe_Deep.Look(ref Snapshot, "snapshot");
            Scribe_Collections.Look(ref PlayerActions, "actions", LookMode.Value);
            Scribe_Collections.Look(ref Events, "events", LookMode.Value);
            Scribe_Values.Look(ref DiffReport, "diff");
            
            if (PlayerActions == null) PlayerActions = new List<string>();
            if (Events == null) Events = new List<string>();
        }
    }
}
