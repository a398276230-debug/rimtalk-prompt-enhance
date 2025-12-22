# Bug 修复记录

## 2025/12/22 - 修复通告功能在非主殖民地地图通用的问题

### 问题描述
通告功能（包括殖民地概况和AI史官总结）在所有地图（包括其他派系的据点）中通用，导致小人在别人家里聊自己殖民地的事情，破坏游戏沉浸感。

### 根本原因
- `ColonyAnnouncementManager` 继承自 `GameComponent`，是游戏级别的全局组件，不区分地图
- `SnapshotService.TakeSnapshot()` 使用 `Find.CurrentMap`，会拍摄当前所在的任何地图
- `AnnouncementBuilder.BuildAnnouncementContext()` 无条件注入所有数据到AI上下文

### 解决方案
添加 `Map.IsPlayerHome` 判断，确保通告功能只在玩家主殖民地生效。

### 修改文件

#### 1. `Source/Services/AnnouncementBuilder.cs`
在 `BuildAnnouncementContext()` 方法开头添加地图检查：
```csharp
// 检查当前地图是否属于玩家殖民地
var map = Find.CurrentMap;
if (map == null || !map.IsPlayerHome)
{
    return null;
}
```
**效果**：只在玩家主殖民地地图上注入通告信息到AI上下文。

#### 2. `Source/Services/SnapshotService.cs`
在 `TakeSnapshot()` 方法中添加地图检查：
```csharp
// 检查当前地图是否属于玩家殖民地
if (!map.IsPlayerHome)
{
    Log.Warning("[RimTalk Enhance] Skipping snapshot - not on player home map.");
    return snapshot;
}
```
**效果**：只在玩家主殖民地地图上拍摄建筑和房间快照。

#### 3. `Source/Models/ColonyAnnouncementManager.cs`
在每日AI总结触发逻辑中添加地图检查：
```csharp
if (currentDay > Data.LastSynthesisDay)
{
    // 检查当前地图是否属于玩家殖民地
    var map = Find.CurrentMap;
    if (map != null && map.IsPlayerHome)
    {
        Log.Message($"[RimTalk Enhance] Triggering daily synthesis. Day: {currentDay}, Last: {Data.LastSynthesisDay}");
        Data.LastSynthesisDay = currentDay;
        _ = MidnightSynthesisService.PerformSynthesis();
    }
    else
    {
        // 如果不在主殖民地，仍然更新日期，避免重复触发
        Data.LastSynthesisDay = currentDay;
        Log.Message($"[RimTalk Enhance] Skipping daily synthesis - not on player home map. Day: {currentDay}");
    }
}
```
**效果**：只在玩家主殖民地地图上触发每日AI总结。即使玩家在其他地图跨天，也会更新日期避免重复触发。

### 测试要点
1. ✅ 在玩家主殖民地，通告功能正常工作
2. ✅ 在其他派系据点或临时地图，AI对话中不会出现殖民地通告信息
3. ✅ 在其他地图跨天时，不会触发AI总结，但会正确更新日期
4. ✅ 返回主殖民地后，通告功能恢复正常

### 技术细节
- 使用 `Map.IsPlayerHome` 属性判断地图归属
- 保持数据结构不变，只在使用时过滤
- 改动最小化，不影响现有存档兼容性

### 编译状态
✅ 编译成功，无错误
