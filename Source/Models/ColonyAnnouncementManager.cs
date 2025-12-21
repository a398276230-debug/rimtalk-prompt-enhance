using System.Collections.Generic;
using System.Linq;
using Verse;
using RimWorld;

namespace RimTalkHealthEnhance
{
    public class ColonyAnnouncementManager : GameComponent
    {
        public static ColonyAnnouncementManager Instance => Current.Game?.GetComponent<ColonyAnnouncementManager>();
        
        public ColonyAnnouncementData Data = new ColonyAnnouncementData();
        
        // 用于UI缓存刷新的版本号
        public int DataVersion { get; private set; } = 0;

        public ColonyAnnouncementManager(Game game)
        {
        }

        public override void GameComponentTick()
        {
            base.GameComponentTick();
            
            // 每2000 ticks (约30秒) 检查一次
            if (Find.TickManager.TicksGame % 2000 == 0)
            {
                CheckAutoCompletion();
            }
        }

        private void CheckAutoCompletion()
        {
            if (Data.Announcements == null) return;
            
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (settings == null) return;

            int currentTick = Find.TickManager.TicksGame;
            var toRemove = new List<ColonyAnnouncement>();

            foreach (var announcement in Data.Announcements)
            {
                // 检查自动完成 (时间)
                if (announcement.Status == AnnouncementStatus.Active && 
                    announcement.DeadlineTicks > 0 && 
                    currentTick > announcement.DeadlineTicks)
                {
                    announcement.Status = AnnouncementStatus.Completed;
                    announcement.CompletedTick = currentTick;
                }
                
                // 检查自动完成 (任务状态)
                if (announcement.Status == AnnouncementStatus.Active && 
                    announcement.RelatedQuestId != -1)
                {
                    var quest = Find.QuestManager.QuestsListForReading.FirstOrDefault(q => q.id == announcement.RelatedQuestId);
                    if (quest != null && (quest.State == QuestState.EndedSuccess || 
                                          quest.State == QuestState.EndedFailed || 
                                          quest.State == QuestState.EndedUnknownOutcome))
                    {
                        announcement.Status = AnnouncementStatus.Completed;
                        announcement.CompletedTick = currentTick;
                    }
                }

                // 检查自动归档（删除）
                if (settings.AutoArchiveCompleted && 
                    announcement.Status == AnnouncementStatus.Completed)
                {
                    // 如果完成超过1天，则删除
                    if (announcement.CompletedTick > 0 && 
                        currentTick > announcement.CompletedTick + 60000)
                    {
                        toRemove.Add(announcement);
                    }
                }
            }

            foreach (var item in toRemove)
            {
                Data.Announcements.Remove(item);
            }
            
            if (toRemove.Count > 0)
                DataVersion++;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Deep.Look(ref Data, "colonyAnnouncementData");
            
            if (Data == null)
                Data = new ColonyAnnouncementData();
        }

        public void AddAnnouncement(ColonyAnnouncement announcement)
        {
            if (Data.Announcements == null)
                Data.Announcements = new List<ColonyAnnouncement>();
                
            Data.Announcements.Add(announcement);
            DataVersion++;
        }

        public void DeleteAnnouncement(string id)
        {
            if (Data.Announcements == null) return;
            
            var item = Data.Announcements.FirstOrDefault(t => t.Id == id);
            if (item != null)
            {
                Data.Announcements.Remove(item);
                DataVersion++;
            }
        }
        
        public void NotifyDataChanged()
        {
            DataVersion++;
        }

        public List<ColonyAnnouncement> GetActiveAnnouncements()
        {
            if (Data.Announcements == null) return new List<ColonyAnnouncement>();
            return Data.Announcements.Where(t => t.Status == AnnouncementStatus.Active).ToList();
        }
    }
}
