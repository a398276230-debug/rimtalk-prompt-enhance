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
        
        public int SnapshotTick;
        
        public void ExposeData()
        {
            Scribe_Collections.Look(ref BuildingCounts, "buildings", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref Rooms, "rooms", LookMode.Deep);
            Scribe_Collections.Look(ref BlueprintCounts, "blueprints", LookMode.Value, LookMode.Value);
            Scribe_Values.Look(ref SnapshotTick, "tick");
            
            if (BuildingCounts == null) BuildingCounts = new Dictionary<string, int>();
            if (Rooms == null) Rooms = new List<RoomSnapshot>();
            if (BlueprintCounts == null) BlueprintCounts = new Dictionary<string, int>();
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
