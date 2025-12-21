# RimTalk - Enhanced Prompt

增强RimTalk的上下文信息显示，提供更详细的健康状况和物品描述。

## 功能特性

### 🏥 健康信息增强
- ✅ **严重度百分比**：显示每个健康状况的严重程度
- ✅ **疼痛等级**：标注疼痛强度（Mild/Moderate/Severe/Extreme）
- ✅ **致命标记**：高亮显示可能致命的健康状况
- ✅ **详细描述**：在Full模式下显示伤病的详细说明
- ✅ **智能优先级**：自动按重要性排序健康问题

### ⚔️ 物品描述增强
- ✅ **装备描述**：显示武器和服装的详细功能描述
- ✅ **携带物品**：显示正在搬运或携带的物品描述
- ✅ **背包物品**：可选显示背包中的物品列表及描述
- ✅ **交互物品**：显示正在使用的建筑/设备描述（如研究台、工作台、床等）
- ✅ **智能过滤**：
  - 自动跳过常见物品（原材料、食物等）
  - **跳过泰南语**：自动过滤高品质物品的艺术描述，只保留功能性描述
  - 可调节品质阈值（如只描述Good及以上品质）

## 输出示例

### 健康信息
```
Health:
- Gunshot wound(Right leg), Severity:85%, Pain:Severe, LETHAL, Desc:A deep penetrating wound caused by gunfire
- Infection(Torso), Severity:45%, Pain:Moderate, LETHAL
```

### 物品信息
```
Equipment:
- Weapon: Plasteel longsword - A masterfully crafted blade of hardened plasteel, deadly in melee combat
- Apparel: Hyperweave duster - Advanced duster woven from cutting-edge hyperweave fibers
- Carrying: Packaged survival meal x5 - High-quality packaged meal designed for long-term survival
- Activity: Researching at Multi-analyzer - An advanced research bench equipped with sensors and computers
```

## 安装要求

- RimWorld 1.5 或 1.6
- [RimTalk](https://steamcommunity.com/sharedfiles/filedetails/?id=3259020985) mod（必须）

## 安装方法

1. 订阅或下载此mod
2. 确保RimTalk已安装
3. 在mod加载顺序中，将此mod放在RimTalk**之后**
4. 重启游戏

## 设置选项

在游戏的 **选项 > Mod设置 > RimTalk Enhanced Prompt** 中可以调整：

### 健康信息设置
- 显示严重度、疼痛等级、致命标记
- 调整疼痛和致命阈值

### 物品描述设置
- **显示选项**：分别控制装备、携带物品、背包物品、交互物品的显示
- **品质阈值**：选择显示描述的最低品质（Awful~Legendary）
- **智能过滤**：
  - 跳过常见物品
  - **跳过艺术描述**：避免提取无意义的艺术故事（泰南语）
- **Token控制**：限制描述长度和物品数量

所有设置都会实时生效，无需重启游戏。

## 编译方法

### 前置要求
- .NET Framework 4.7.2 或更高版本
- Visual Studio 2019+ 或 Rider

### 步骤
1. 确保RimTalk mod已安装在 `../RimTalk/` 目录
2. 打开 `Source/RimTalkHealthEnhance.csproj`
3. 构建项目（Release配置）
4. DLL将自动输出到 `1.6/Assemblies/` 目录

或使用命令行：
```bash
cd Source
dotnet build -c Release
```

## 技术说明

此mod使用Harmony前缀补丁替换RimTalk的 `ContextBuilder` 方法，不会修改原mod的任何文件。

### 主要组件

- **HealthContextPatch**: 增强健康信息构建
- **EquipmentContextPatch**: 增强装备和物品信息构建（含交互物品检测）
- **ItemDescriptionBuilder**: 智能物品描述提取器（含泰南语过滤器）

## 兼容性

- ✅ 与RimTalk完全兼容
- ✅ 不修改游戏核心文件
- ✅ 可以随时启用/禁用
- ✅ 支持多语言（继承RimWorld的翻译）

## 许可证

MIT License

## 作者

ruaji

## 反馈

如有问题或建议，请在Steam创意工坊页面留言。
