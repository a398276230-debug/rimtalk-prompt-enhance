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
        
        /// <summary>
        /// 获取游戏内第几天（从0开始，用于显示"第X天"）
        /// </summary>
        public int GameDay => GenDate.DaysPassedAt(GenDate.TickAbsToGame((int)AbsTick));
        
        /// <summary>
        /// 获取显示用的日期字符串（如"5500年赫象第6天"）
        /// </summary>
        public string GetDateString(Vector2 location)
        {
            return GenDate.DateFullStringAt(AbsTick, location);
        }
        
        /// <summary>
        /// 获取显示用的日期字符串（带偏移量）
        /// </summary>
        public string GetDateStringWithOffset(long tickOffset, Vector2 location)
        {
            return GenDate.DateFullStringAt(AbsTick + tickOffset, location);
        }
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref AbsTick, "absTick", 0L);
            
            // 兼容旧版本：如果 AbsTick 为0，尝试从旧字段恢复
            #pragma warning disable CS0612 // 禁用弃用警告
            Scribe_Values.Look(ref Day, "day");
            Scribe_Values.Look(ref Tick, "tick");
            #pragma warning restore CS0612
            
            // 如果是加载旧存档，从 Tick 转换到 AbsTick
            if (Scribe.mode == LoadSaveMode.PostLoadInit && AbsTick == 0 && Tick > 0)
            {
                AbsTick = GenDate.TickGameToAbs(Tick);
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
