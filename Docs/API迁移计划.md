# RimTalk 增强提示词 - API迁移计划

## 1. 背景

RimTalk 现已提供官方 API (`RimTalkPromptAPI`) 用于注册胡子模板变量(Mustache)和提示词条目(Entry)，替代原有的 Harmony Patch 方式。本文档详细说明如何将现有的 Patch 迁移到新 API。

## 2. RimTalk API 概览

### 2.1 可用的 ContextCategories

**Pawn Categories** (`ContextCategories.Pawn.*`):
- `Name`, `FullName`, `Gender`, `Age`, `Race`, `Title`
- `Faction`, `Role`, `Job`, `Personality`
- `Mood`, `MoodPercent`, `Profile`, `Backstory`
- `Traits`, `Skills`, `Health`, `Thoughts`
- `Relations`, `Equipment`, `Genes`, `Ideology`, `CaptiveStatus`

**Environment Categories** (`ContextCategories.Environment.*`):
- `Time`, `Date`, `Season`, `Weather`, `Temperature`
- `Location`, `Terrain`, `Beauty`, `Cleanliness`
- `Surroundings`, `Wealth`

### 2.2 API 方法

```csharp
// Hook 注册 (修改现有值)
RegisterPawnHook(modId, category, operation, handler, priority)
RegisterEnvironmentHook(modId, category, operation, handler, priority)
// operation: Append | Prepend | Override

// Section 注入 (添加新section)
InjectPawnSection(modId, sectionName, anchor, position, provider, priority)
InjectEnvironmentSection(modId, sectionName, anchor, position, provider, priority)
// position: Before | After

// 变量注册 (添加新变量)
RegisterPawnVariable(modId, variableName, provider, description, priority)
RegisterEnvironmentVariable(modId, variableName, provider, description, priority)
RegisterContextVariable(modId, variableName, provider, description, priority)

// Prompt Entry (添加提示词条目)
AddPromptEntry(entry)
InsertPromptEntry(entry, index)
InsertPromptEntryAfter(entry, afterEntryId)
InsertPromptEntryBefore(entry, beforeEntryId)
CreatePromptEntry(name, content, role, position, inChatDepth, sourceModId)
```

## 3. 迁移对照表

### 3.1 ✅ 需要迁移的 Patch

| 当前 Patch | 新 API 调用 | 操作类型 | 说明 |
|-----------|------------|---------|------|
| `HealthContextPatch` | `RegisterPawnHook(Health, Override)` | Override | 完全替换健康上下文 |
| `EquipmentContextPatch` | `RegisterPawnHook(Equipment, Override)` | Override | 完全替换装备上下文 |
| `RelationsContextPatch` | `RegisterPawnHook(Relations, Override)` | Override | 解除关系数量限制 |
| `TraitsContextPatch` | `RegisterPawnHook(Traits, Override)` | Override | 解除特质数量限制 |
| `LocationContextPatch` | `RegisterEnvironmentHook(Location, Append)` | Append | 追加相对位置信息 |
| `ActivityStatusPatch` | `RegisterPawnHook(Job, Override)` | Override | 添加"移动中"状态 |
| `AnnouncementContextPatch` | `InjectEnvironmentSection` | Inject | 注入殖民地状况板 |
| `PromptSnapshotPatch` | `AddPromptEntry` | Entry | 注入快照到Prompt |

### 3.2 🔴 保留 Harmony Patch (无对应API)

| Patch | 功能 | 原因 |
|-------|------|------|
| `ArchiveCapturePatch` | 拦截 Archive.Add 捕获事件 | 事件监听，无API |
| `BlueprintActionPatch` | 拦截蓝图放置/取消 | 事件监听，无API |
| `PawnKillPatch` | 拦截死亡/倒地事件 | 事件监听，无API |
| `PawnExitMapPatch` | 拦截离开地图事件 | 事件监听，无API |
| `PawnDamagePatch` | 拦截伤害事件 | 事件监听，无API |
| `LayoutCachePatch` | 房间/建筑变化触发缓存失效 | 事件监听，无API |
| `PlaySettingsColonyCenterPatch` | UI按钮注入 | UI修改，无API |
| `WeatherTransitionPatch` | 天气变化追踪 | 事件监听，无API |
| `GameConditionCapturePatch` | 游戏条件捕获 | 事件监听，无API |

## 4. 新增文件结构

```
Source/
├── API/
│   └── RimTalkEnhanceAPI.cs      # API注册服务（新增）
│
├── Patches/                       # 保留事件监听类patch
│   ├── ArchiveCapturePatch.cs     # 保留
│   ├── BlueprintActionPatch.cs    # 保留
│   ├── PawnKillPatch.cs           # 保留
│   ├── PawnExitMapPatch.cs        # 保留
│   ├── PawnDamagePatch.cs         # 保留
│   ├── LayoutCachePatch.cs        # 保留
│   ├── PlaySettingsColonyCenterPatch.cs # 保留
│   ├── WeatherTransitionPatch.cs  # 保留
│   ├── GameConditionCapturePatch.cs # 保留
│   │
│   └── [已删除]                   # 迁移后删除
│       ├── HealthContextPatch.cs
│       ├── EquipmentContextPatch.cs
│       ├── RelationsContextPatch.cs
│       ├── TraitsContextPatch.cs
│       ├── LocationContextPatch.cs
│       ├── ActivityStatusPatch.cs
│       ├── AnnouncementContextPatch.cs
│       └── PromptSnapshotPatch.cs
```

## 5. RimTalkEnhanceAPI 设计

```csharp
using RimTalk.API;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// RimTalk 增强提示词 API 注册服务
    /// 在游戏启动时注册所有 hook 和 entry
    /// </summary>
    public static class RimTalkEnhanceAPI
    {
        private const string MOD_ID = "RimTalkHealthEnhance";
        private static bool _registered = false;

        /// <summary>
        /// 在 Mod 初始化时调用
        /// </summary>
        public static void Initialize()
        {
            if (_registered) return;
            
            try
            {
                RegisterPawnHooks();
                RegisterEnvironmentHooks();
                RegisterSections();
                RegisterPromptEntries();
                
                _registered = true;
                Log.Message("[RimTalk Enhance] API hooks registered successfully");
            }
            catch (Exception ex)
            {
                Log.Error($"[RimTalk Enhance] Failed to register API hooks: {ex}");
            }
        }

        /// <summary>
        /// Mod 卸载时调用
        /// </summary>
        public static void Cleanup()
        {
            if (!_registered) return;
            RimTalkPromptAPI.UnregisterAllHooks(MOD_ID);
            _registered = false;
        }

        #region Pawn Hooks
        
        private static void RegisterPawnHooks()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;

            // Health Hook (Override)
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Health,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) => {
                    if (!Settings.Get().Context.IncludeHealth) return null;
                    return HealthInfoBuilder.BuildEnhancedHealthContext(pawn, GetInfoLevel());
                }
            );

            // Equipment Hook (Override)
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Equipment,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) => {
                    if (!Settings.Get().Context.IncludeEquipment) return null;
                    return BuildEnhancedEquipmentContext(pawn, GetInfoLevel());
                }
            );

            // Relations Hook (Override) - 解除数量限制
            if (settings.UnlimitedRelations)
            {
                RimTalkPromptAPI.RegisterPawnHook(
                    MOD_ID,
                    ContextCategories.Pawn.Relations,
                    ContextHookRegistry.HookOperation.Override,
                    (pawn, original) => {
                        if (!Settings.Get().Context.IncludeRelations) return original;
                        return GetRelationsStringUnlimited(pawn);
                    }
                );
            }

            // Traits Hook (Override) - 解除数量限制
            if (settings.UnlimitedTraits)
            {
                RimTalkPromptAPI.RegisterPawnHook(
                    MOD_ID,
                    ContextCategories.Pawn.Traits,
                    ContextHookRegistry.HookOperation.Override,
                    (pawn, original) => {
                        if (!Settings.Get().Context.IncludeTraits) return original;
                        return GetTraitsContextUnlimited(pawn, GetInfoLevel());
                    }
                );
            }

            // Job Hook (Override) - 添加移动状态
            // 注意: RimTalk 内置已有移动检测，此hook可能只需要在特定情况下使用
            RimTalkPromptAPI.RegisterPawnHook(
                MOD_ID,
                ContextCategories.Pawn.Job,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) => {
                    return GetEnhancedActivity(pawn, original);
                }
            );
        }

        #endregion

        #region Environment Hooks

        private static void RegisterEnvironmentHooks()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;

            // Location Hook (Append) - 追加相对位置
            if (settings.ShowRelativeLocation)
            {
                RimTalkPromptAPI.RegisterEnvironmentHook(
                    MOD_ID,
                    ContextCategories.Environment.Location,
                    ContextHookRegistry.HookOperation.Append,
                    (map, original) => {
                        // 找到当前对话的主pawn
                        var mainPawn = GetCurrentMainPawn(map);
                        if (mainPawn == null) return original;
                        
                        string relativeLocation = LocationContextBuilder.GetRelativeLocation(mainPawn);
                        if (!string.IsNullOrEmpty(relativeLocation))
                        {
                            string prefix = mainPawn.Map.IsPlayerHome 
                                ? "Relative Position" 
                                : "Current Map";
                            return original + $"\n{prefix}: {relativeLocation}";
                        }
                        return original;
                    }
                );
            }
        }

        #endregion

        #region Section Injection

        private static void RegisterSections()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;

            // 殖民地状况板 - 注入为新的环境section
            if (settings.ShowColonyAnnouncements)
            {
                RimTalkPromptAPI.InjectEnvironmentSection(
                    MOD_ID,
                    "colony_status",
                    ContextCategories.Environment.Wealth, // 锚定在Wealth之后
                    ContextHookRegistry.InjectPosition.After,
                    (map) => AnnouncementBuilder.BuildAnnouncementContext(),
                    priority: 100
                );
            }
        }

        #endregion

        #region Prompt Entries

        private static void RegisterPromptEntries()
        {
            // 快照注入到 Prompt
            // 注意: 这需要在对话触发时动态添加，可能需要其他机制
            // 暂时保留原有的 PromptSnapshotPatch 或使用其他方式
        }

        #endregion

        #region Helper Methods

        private static PromptService.InfoLevel GetInfoLevel()
        {
            // 从 RimTalk 设置获取当前的 InfoLevel
            return Settings.Get().ContextInfoLevel;
        }

        private static Pawn GetCurrentMainPawn(Map map)
        {
            // 尝试获取当前对话的主 Pawn
            // 这可能需要通过其他方式获取
            return null; // TODO: 实现
        }

        // 复制自 RelationsContextPatch
        private static string GetRelationsStringUnlimited(Pawn pawn) 
        {
            // ... 原有逻辑
        }

        // 复制自 TraitsContextPatch
        private static string GetTraitsContextUnlimited(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            // ... 原有逻辑
        }

        // 复制自 EquipmentContextPatch
        private static string BuildEnhancedEquipmentContext(Pawn pawn, PromptService.InfoLevel infoLevel)
        {
            // ... 原有逻辑
        }

        // 活动状态增强
        private static string GetEnhancedActivity(Pawn pawn, string original)
        {
            // 检查是否需要添加移动状态前缀
            // RimTalk 内置已有 "on the way to" 检测，可能不需要额外处理
            return original;
        }

        #endregion
    }
}
```

## 6. 初始化集成

修改 `RimTalkHealthEnhanceStartup.cs`:

```csharp
[StaticConstructorOnStartup]
public static class RimTalkHealthEnhanceStartup
{
    static RimTalkHealthEnhanceStartup()
    {
        var harmony = new Harmony("RimTalkHealthEnhance");
        
        // 只加载保留的 Harmony patches
        // 事件监听类 patch 仍使用 Harmony
        harmony.PatchAll(typeof(ArchiveCapturePatch).Assembly);
        
        // 注册 RimTalk API hooks
        RimTalkEnhanceAPI.Initialize();
        
        Log.Message("[RimTalk Enhance] Initialized with API hooks");
    }
}
```

## 7. 迁移步骤

### Phase 1: 准备工作
1. 创建 `Source/API/RimTalkEnhanceAPI.cs`
2. 添加 RimTalk API 引用

### Phase 2: 迁移 Pawn Hooks
1. 迁移 `HealthContextPatch` → `RegisterPawnHook(Health, Override)`
2. 迁移 `EquipmentContextPatch` → `RegisterPawnHook(Equipment, Override)`
3. 迁移 `RelationsContextPatch` → `RegisterPawnHook(Relations, Override)`
4. 迁移 `TraitsContextPatch` → `RegisterPawnHook(Traits, Override)`
5. 评估 `ActivityStatusPatch` → `RegisterPawnHook(Job, Override)` (可能不需要)

### Phase 3: 迁移 Environment Hooks
1. 迁移 `LocationContextPatch` → `RegisterEnvironmentHook(Location, Append)`

### Phase 4: 迁移 Section/Entry
1. 迁移 `AnnouncementContextPatch` → `InjectEnvironmentSection`
2. 评估 `PromptSnapshotPatch` → `AddPromptEntry` (需要动态注入机制)

### Phase 5: 清理
1. 删除已迁移的 Patch 文件
2. 更新 Harmony 加载逻辑
3. 测试所有功能

## 8. 注意事项

### 8.1 兼容性
- 新 API 需要 RimTalk 最新版本支持
- 建议添加版本检测，旧版 RimTalk 回退到 Harmony patch

### 8.2 动态注入问题
- `PromptSnapshotPatch` 需要在对话触发时动态注入快照
- `AddPromptEntry` 是静态添加，可能需要配合 `ContextVariable` 实现动态内容

### 8.3 获取当前 Pawn 问题
- Environment hooks 只接收 Map 参数
- 需要找到方法获取当前对话的主 Pawn (用于 Location 增强)

### 8.4 ActivityStatusPatch 评估
- RimTalk 内置已有移动状态检测 ("on the way to...")
- 检查现有功能是否重复，可能可以完全移除此 patch

## 9. 测试清单

- [ ] Health hook 正确替换健康信息
- [ ] Equipment hook 正确显示装备/携带物品/交互物品
- [ ] Relations hook 正确显示所有关系（无数量限制）
- [ ] Traits hook 正确显示所有特质（无数量限制）
- [ ] Location hook 正确追加相对位置信息
- [ ] 殖民地状况板正确注入到上下文
- [ ] 快照正确注入到 Prompt
- [ ] 保留的 Harmony patch 正常工作
- [ ] 设置开关正确控制各功能启用/禁用

## 10. 预期收益

1. **更好的兼容性**: 使用官方 API，减少与 RimTalk 更新的冲突
2. **更清晰的架构**: Hook 和 Entry 分离，代码更易维护
3. **更少的反射**: 不再需要 Harmony 拦截内部方法
4. **更好的优先级控制**: API 支持 priority 参数，可精确控制执行顺序