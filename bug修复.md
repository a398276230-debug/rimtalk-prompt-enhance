# Bug 修复记录

## 2025/12/24 - 修复 AI 连接时的字符编码错误

### 问题描述
部分用户在测试 AI 连接或使用 AI 功能时遇到以下错误：
```
[RimTalk Enhance] AI Call Exception: Illegal byte sequence encounted in the input.
Parameter name: string
```

### 根本原因
这是一个 .NET `HttpClient` 在处理包含非 ASCII 字符（如中文）的计算机名时的已知问题。
1. 当计算机名包含中文字符时，系统会自动将其注入到 HTTP 请求头（如 User-Agent 或 Host）中。
2. HTTP 协议标准要求头部字段必须是 ASCII 字符。
3. `HttpClient` 在构建请求时尝试编码这些字符，导致 `DecoderFallbackException`。

### 解决方案
通过配置 `HttpClientHandler` 和手动管理请求头，最大限度地减少系统环境变量的自动注入：

1. **配置 HttpClientHandler**：
   - `UseDefaultCredentials = false`：防止自动使用系统凭据
   - `PreAuthenticate = false`：禁用预认证
   - `UseCookies = false`：禁用 Cookie

2. **清理请求头**：
   - `client.DefaultRequestHeaders.Clear()`：清除所有默认头
   - 手动设置 `User-Agent` 和 `Accept` 头，确保请求头纯净

3. **增强错误提示**：
   - 添加了对 `DecoderFallbackException` 和特定错误消息的检测
   - 如果检测到此类错误，输出详细的日志，明确指出可能是计算机名问题，并给出解决方案

### 修改文件

#### `Source/Services/SimpleAIClient.cs`
```csharp
// Configure HttpClientHandler to minimize system environment interference
var handler = new HttpClientHandler
{
    UseDefaultCredentials = false,
    PreAuthenticate = false,
    UseCookies = false,
    AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
};

using (var client = new HttpClient(handler))
{
    // Clear default headers to avoid auto-injection of system info
    client.DefaultRequestHeaders.Clear();
    client.DefaultRequestHeaders.Add("User-Agent", "RimTalk-Enhance/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
    
    // ...
    
    catch (Exception ex)
    {
        // Enhanced error handling for encoding issues
        if (ex is System.Text.DecoderFallbackException || 
            ex.Message.Contains("Illegal byte sequence") ||
            ex.Message.Contains("encounted in the input"))
        {
            Log.Error("[RimTalk Enhance] Character encoding error detected.");
            Log.Warning($"[RimTalk Enhance] This is likely caused by non-ASCII characters in your computer name: {System.Environment.MachineName}");
            // ...
        }
        // ...
    }
}
```

### 效果
- ✅ 尝试从技术上规避系统环境变量导致的编码问题
- ✅ 如果问题仍然存在，提供清晰、有用的错误提示和解决方案
- ✅ 不影响正常用户的 AI 连接功能

### 编译状态
✅ 编译成功，无错误

---

## 2025/12/22 - 修复自动捕获任务完成后仍挂在 Context 的问题

### 问题描述
自动捕获的任务（如袭击、信件等事件）在点击"完成"按钮后，仍然会被注入到 AI 的对话上下文中，导致 AI 继续询问已处理完的事件。

### 根本原因
在 `AnnouncementBuilder.cs` 的过滤逻辑中，所有已完成的任务（无论是手动创建还是自动捕获）都会在 `CompletedTaskShowDays`（默认1天）内继续注入到 Context。这对于手动创建的工程类任务是合理的（让 AI 知道项目已完成），但对于自动捕获的瞬时事件（如袭击、信件）来说，用户点击"完成"通常意味着"我知道了，不要再告诉 AI 了"。

### 解决方案
在 `AnnouncementBuilder.cs` 中区分手动任务和自动捕获任务的处理逻辑：
- **自动捕获任务**：完成后立即从 Context 中移除
- **手动任务**：保持原有逻辑（保留 `CompletedTaskShowDays` 天）

### 修改文件

#### `Source/Services/AnnouncementBuilder.cs`
在已完成任务的过滤逻辑中添加 `IsAutoCaptured` 判断：
```csharp
if (settings.OnlyShowActiveTasks && t.Status == AnnouncementStatus.Completed && t.CompletedTick > 0)
{
    // 自动捕获的事件完成后立即不再注入到 Context
    if (t.IsAutoCaptured)
    {
        return false;
    }
    
    // 手动创建的任务保持原有逻辑（保留指定天数）
    int ticksSinceCompleted = Find.TickManager.TicksGame - t.CompletedTick;
    return ticksSinceCompleted <= (int)(settings.CompletedTaskShowDays * 60000);
}
```

### 效果
- ✅ 自动捕获的事件（袭击、信件等）点击"完成"后立即从 AI 上下文中消失
- ✅ 手动创建的任务（工程、人员安排等）完成后仍会保留指定天数，让 AI 知道任务已完成
- ✅ 不影响 UI 显示和数据删除逻辑（仍由 `AutoCapturedDeleteDays` 控制）

### 编译状态
✅ 编译成功，无错误

---

## 2025/12/23 - 修复派系信息构建器的线程安全问题

### 问题描述
在异步调用 `AIService.UpdateContext()` 时报错：
```
[Director] Generation failed: Accessing map pawns off main thread - this is never allowed due to list pooling and will result in modification exceptions elsewhere in code.
```

### 根本原因
调用链路：
1. `AIService.UpdateContext()` 在后台线程异步调用
2. `AnnouncementContextPatch.Prefix()` 拦截并调用 `AnnouncementBuilder.BuildAnnouncementContext()`
3. `AnnouncementBuilder` 调用 `FactionInfoBuilder.BuildFactionContext()`
4. `FactionInfoBuilder.GetFactionsOnMap()` 访问 `map.mapPawns.AllPawns` ❌ **线程冲突**

RimWorld 的 `map.mapPawns` 使用了对象池（list pooling），严格禁止在非主线程访问，否则会导致修改异常。

### 解决方案
采用**定时缓存策略**，在主线程定期更新派系信息，异步调用时直接读取缓存：

1. **主线程更新**：`ColonyAnnouncementManager.GameComponentTick()` 每 N 秒调用 `UpdateFactionCache()`
2. **缓存存储**：调用 `FactionInfoBuilder.BuildFactionContextUnsafe()` 访问 `map.mapPawns` 并缓存结果
3. **异步读取**：`FactionInfoBuilder.BuildFactionContext()` 直接返回缓存的字符串
4. **用户可配置**：更新间隔可在设置中调整（1-30秒，默认5秒）

### 修改文件

#### 1. `Source/Settings/HealthEnhanceSettings.cs`
添加缓存更新间隔设置：
```csharp
// === Faction Relations Settings ===
public float FactionCacheUpdateInterval = 5f;  // 派系信息缓存更新间隔（秒）
```

在 `ExposeData()` 中保存：
```csharp
Scribe_Values.Look(ref FactionCacheUpdateInterval, "factionCacheUpdateInterval", 5f);
```

在 `DoFactionSettingsWindowContents()` 中添加UI控件：
```csharp
Widgets.Label(listing.GetRect(22f), $"缓存更新间隔: {FactionCacheUpdateInterval:F1} 秒");
FactionCacheUpdateInterval = listing.Slider(FactionCacheUpdateInterval, 1f, 30f);
```

#### 2. `Source/Models/ColonyAnnouncementManager.cs`
添加缓存字段和更新逻辑：
```csharp
// 派系信息缓存（线程安全）
private string _cachedFactionInfo = null;
private int _lastFactionUpdateTick = 0;

public override void GameComponentTick()
{
    // ... 现有代码 ...
    
    // 定期更新派系信息缓存（线程安全）
    var settings = RimTalkHealthEnhanceMod.Settings;
    if (settings != null && settings.ShowFactionRelations)
    {
        int updateInterval = (int)(settings.FactionCacheUpdateInterval * 60); // 转换为 ticks
        if (currentTick - _lastFactionUpdateTick >= updateInterval)
        {
            UpdateFactionCache();
            _lastFactionUpdateTick = currentTick;
        }
    }
}

/// <summary>
/// 更新派系信息缓存（在主线程调用）
/// </summary>
public void UpdateFactionCache()
{
    _cachedFactionInfo = FactionInfoBuilder.BuildFactionContextUnsafe();
}

/// <summary>
/// 获取缓存的派系信息（线程安全）
/// </summary>
public string GetCachedFactionInfo()
{
    return _cachedFactionInfo;
}
```

#### 3. `Source/Services/FactionInfoBuilder.cs`
重构为两个方法：
```csharp
/// <summary>
/// 线程安全的公共方法 - 从缓存读取
/// </summary>
public static string BuildFactionContext()
{
    var manager = ColonyAnnouncementManager.Instance;
    if (manager == null) return null;
    
    return manager.GetCachedFactionInfo();
}

/// <summary>
/// 不安全的方法 - 仅在主线程调用（由 Manager 定期更新缓存）
/// </summary>
public static string BuildFactionContextUnsafe()
{
    // 原有的 BuildFactionContext() 代码
    // 访问 map.mapPawns.AllPawns 等游戏对象
}
```

#### 4. `Source/Services/AnnouncementBuilder.cs`
无需修改，继续调用 `FactionInfoBuilder.BuildFactionContext()`（现在是线程安全的）

### 技术细节
- **性能开销**：派系信息构建只遍历地图 Pawn（通常几十到几百个），性能消耗极小
- **更新频率**：默认5秒更新一次，即使1秒一次也完全没问题
- **数据实时性**：派系关系不会秒级变化，5秒延迟完全可接受
- **线程安全**：异步调用时只读取字符串，不访问游戏对象

### 效果
- ✅ 完全线程安全，不会再出现 "Accessing map pawns off main thread" 错误
- ✅ 性能开销极小（5秒更新一次）
- ✅ 用户可自定义更新频率（1-30秒）
- ✅ 数据实时性足够（派系关系变化不频繁）
- ✅ 无需修改调用方代码

### 编译状态
✅ 编译成功，无错误

---

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
