using System.Collections.Generic;
using Verse;

namespace RimTalkHealthEnhance
{
    public class ColonySnapshot : IExposable
    {
        // 建筑快照: DefName -> Count
        public Dictionary<string, int> BuildingCounts = new Dictionary<string, int>();
        
        // 房间快照
        public List<RoomSnapshot> Rooms = new List<RoomSnapshot>();
        
        // 蓝图快照: DefName -> Count
        public Dictionary<string, int> BlueprintCounts = new Dictionary<string, int>();
        
        // 蓝图与工程的关联: DefName -> ProjectListWrapper
        public Dictionary<string, ProjectListWrapper> BlueprintToProjects = new Dictionary<string, ProjectListWrapper>();
        
        // 正在重新安装的建筑: DefName -> Count (使用 RimWorld 内置的 reinstallationMap)
        public Dictionary<string, int> ReinstallingCounts = new Dictionary<string, int>();
        
        public int SnapshotTick;
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref BuildingCounts, "buildings", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref Rooms, "rooms", LookMode.Deep);
            Scribe_Collections.Look(ref BlueprintCounts, "blueprints", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref BlueprintToProjects, "blueprintToProjects", LookMode.Value, LookMode.Deep);
            Scribe_Collections.Look(ref ReinstallingCounts, "reinstalling", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref SnapshotTick, "tick");
            
            if (BuildingCounts == null) BuildingCounts = new Dictionary<string, int>();
            if (Rooms == null) Rooms = new List<RoomSnapshot>();
            if (BlueprintCounts == null) BlueprintCounts = new Dictionary<string, int>();
            if (BlueprintToProjects == null) BlueprintToProjects = new Dictionary<string, ProjectListWrapper>();
            if (ReinstallingCounts == null) ReinstallingCounts = new Dictionary<string, int>();
        }
    }

    public class ProjectListWrapper : IExposable
    {
        public List<string> Projects = new List<string>();
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref Projects, "projects", LookMode.Value);
            if (Projects == null) Projects = new List<string>();
        }
    }

    public class RoomSnapshot : IExposable
    {
        public string RoomRole;  // "Bedroom", "Kitchen", etc.
        public int CellCount;
        public List<string> KeyBuildings = new List<string>();  // 房间内的关键建筑 Label
        
        public void ExposeData()
        {
            Scribe_Values.Look(ref RoomRole, "role");
            Scribe_Values.Look(ref CellCount, "size");
            Scribe_Collections.Look(ref KeyBuildings, "buildings", LookMode.Value);
            
            if (KeyBuildings == null) KeyBuildings = new List<string>();
        }
    }
}
