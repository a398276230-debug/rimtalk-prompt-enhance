using System.Collections.Generic;
using Verse;

namespace RimTalkHealthEnhance
{
    public class DailySnapshot : IExposable
    {
        public int Day;  // 游戏天数
        public int Tick;  // 快照时间戳
        
        // AI 生成的总结
        public string AISummary = "";
        
        // 结构化数据
        public ColonySnapshot Snapshot;
        public List<string> PlayerActions = new List<string>();  // 玩家操作日志
        public List<string> Events = new List<string>();  // 当日事件
        
        // 差分报告（原始文本）
        public string DiffReport = "";
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref Day, "day");
            Scribe_Values.Look(ref Tick, "tick");
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
