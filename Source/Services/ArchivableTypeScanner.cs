using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 主动扫描所有可归档事件类型（C# 类型 + Letter/Message defName），构建层级树供设置 UI 使用。
    /// 机制参考上游 RimTalk 的 Settings_EventFilter，但数据完全独立：
    /// 只读写本项目 AutoCaptureSettings.EnabledEventTypes，与 RimTalk 的播报过滤互不影响。
    /// </summary>
    public static class ArchivableTypeScanner
    {
        public const string Core = "Core";
        public const string VerseMessage = "Verse.Message";

        private static Dictionary<string, List<string>> _typeHierarchy = new Dictionary<string, List<string>>();
        private static Dictionary<string, string> _sourceMap = new Dictionary<string, string>();
        private static HashSet<string> _messageOnlyTypes = new HashSet<string>();
        private static bool _scanned;

        /// <summary>父类型（C# 类型名）-> 子 defName 列表，用于层级树 UI</summary>
        public static IReadOnlyDictionary<string, List<string>> TypeHierarchy => _typeHierarchy;

        /// <summary>类型/defName -> 来源 mod 名（Core 则省略显示）</summary>
        public static IReadOnlyDictionary<string, string> SourceMap => _sourceMap;

        /// <summary>UI 折叠状态（父类型 -> 是否展开）</summary>
        public static readonly HashSet<string> ExpandedParents = new HashSet<string>();

        public static bool Scanned => _scanned;

        public static void ScanIfNeeded()
        {
            if (!_scanned) Scan();
        }

        public static void Scan()
        {
            var archivableTypes = new HashSet<string>();
            _typeHierarchy = new Dictionary<string, List<string>>();
            _sourceMap = new Dictionary<string, string>();
            var likelyCoreTypes = new HashSet<string>();
            var letterDefNames = new HashSet<string>();
            var messageDefNames = new HashSet<string>();

            // mod 程序集 -> mod 名映射
            var assemblyToMod = new Dictionary<Assembly, string>();
            foreach (var mod in LoadedModManager.RunningMods)
            {
                foreach (var asm in mod.assemblies.loadedAssemblies)
                {
                    if (!assemblyToMod.ContainsKey(asm))
                        assemblyToMod[asm] = mod.Name;
                }
            }

            // 1. 扫描所有程序集中的 IArchivable 实现（父类型/机制）
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    string modName = null;
                    bool isLikelyCore = false;

                    if (assemblyToMod.TryGetValue(assembly, out var mName))
                    {
                        modName = mName;
                    }
                    else
                    {
                        var asmName = assembly.GetName().Name;
                        if (asmName.StartsWith("Assembly-CSharp") ||
                            asmName.StartsWith("RimWorld") ||
                            asmName.StartsWith("Verse") ||
                            asmName.StartsWith("UnityEngine"))
                        {
                            isLikelyCore = true;
                        }
                    }

                    var types = assembly.GetTypes()
                        .Where(t => typeof(IArchivable).IsAssignableFrom(t) &&
                                    !t.IsInterface &&
                                    !t.IsAbstract)
                        .Select(t => t.FullName)
                        .ToList();

                    foreach (var type in types)
                    {
                        archivableTypes.Add(type);
                        if (!_typeHierarchy.ContainsKey(type))
                            _typeHierarchy[type] = new List<string>();

                        if (modName != null)
                            _sourceMap[type] = modName;

                        if (isLikelyCore)
                            likelyCoreTypes.Add(type);
                    }
                }
                catch (Exception)
                {
                    // 忽略无法加载的程序集
                }
            }

            // 2. 扫描当前存档 Archive 中的实际实例（补充遗漏）
            if (Current.Game != null && Find.Archive != null)
            {
                foreach (var archivable in Find.Archive.ArchivablesListForReading)
                {
                    var typeName = archivable.GetType().FullName;
                    archivableTypes.Add(typeName);
                    if (!_typeHierarchy.ContainsKey(typeName))
                        _typeHierarchy[typeName] = new List<string>();
                }
            }

            // 3. 扫描 LetterDef（父类型 = letterClass，子项 = defName）
            foreach (var def in DefDatabase<LetterDef>.AllDefs)
            {
                var parentType = def.letterClass?.FullName;
                if (string.IsNullOrEmpty(parentType)) continue;

                letterDefNames.Add(def.defName);
                archivableTypes.Add(parentType);
                archivableTypes.Add(def.defName);

                if (!_typeHierarchy.ContainsKey(parentType))
                    _typeHierarchy[parentType] = new List<string>();

                if (!_typeHierarchy[parentType].Contains(def.defName))
                    _typeHierarchy[parentType].Add(def.defName);

                string defSource = def.modContentPack?.Name ?? Core;
                _sourceMap[def.defName] = defSource;

                if (!_sourceMap.ContainsKey(parentType))
                    _sourceMap[parentType] = defSource;
            }

            // 4. 扫描 MessageTypeDef（父类型统一为 Verse.Message）
            foreach (var def in DefDatabase<MessageTypeDef>.AllDefs)
            {
                messageDefNames.Add(def.defName);
                archivableTypes.Add(VerseMessage);
                archivableTypes.Add(def.defName);

                if (!_typeHierarchy.ContainsKey(VerseMessage))
                    _typeHierarchy[VerseMessage] = new List<string>();

                if (!_typeHierarchy[VerseMessage].Contains(def.defName))
                    _typeHierarchy[VerseMessage].Add(def.defName);

                _sourceMap[def.defName] = def.modContentPack?.Name ?? Core;

                if (!_sourceMap.ContainsKey(VerseMessage))
                    _sourceMap[VerseMessage] = Core;
            }

            // 5. 未标明来源的核心类型回填 Core
            foreach (var type in archivableTypes.Where(t => !_sourceMap.ContainsKey(t) && likelyCoreTypes.Contains(t)))
            {
                _sourceMap[type] = Core;
            }

            // 去重：同一 defName 同时是 LetterDef 和 MessageTypeDef 时只挂在 Verse.Message 下
            if (_typeHierarchy.TryGetValue(VerseMessage, out var msgChildren))
            {
                var messageKeys = new HashSet<string>(msgChildren);
                foreach (var parent in _typeHierarchy.Keys.ToList())
                {
                    if (parent == VerseMessage) continue;
                    _typeHierarchy[parent].RemoveAll(child => messageKeys.Contains(child));
                }
            }

            // 6. 写入发现列表并填充默认值
            AutoCaptureSettings.DiscoveredEventTypes = archivableTypes.OrderBy(x => x).ToList();

            foreach (var key in _typeHierarchy.Keys.ToList())
            {
                _typeHierarchy[key].Sort();
            }

            // 默认值策略：纯 Message 类型默认关（避免刷屏）；
            // Letter/Message 同名的 defName（ThreatBig 等）以 Letter 语义优先默认开，
            // 保证袭击追踪/战报等核心功能开箱可用。
            _messageOnlyTypes = new HashSet<string> { VerseMessage };
            foreach (var messageDefName in messageDefNames)
            {
                if (!letterDefNames.Contains(messageDefName))
                    _messageOnlyTypes.Add(messageDefName);
            }

            var settings = RimTalkHealthEnhanceMod.Settings;
            var enabledTypes = settings.EnabledEventTypes;

            foreach (var typeName in AutoCaptureSettings.DiscoveredEventTypes)
            {
                if (!enabledTypes.ContainsKey(typeName))
                {
                    enabledTypes[typeName] = !_messageOnlyTypes.Contains(typeName);
                }
            }

            _scanned = true;

            Log.Message($"[RimTalk Enhance] Discovered {AutoCaptureSettings.DiscoveredEventTypes.Count} archivable types across {_typeHierarchy.Count} parent categories.");
        }

        /// <summary>纯 Message 相关类型（Verse.Message 及仅作为 MessageTypeDef 存在的 defName），这些默认关闭</summary>
        public static HashSet<string> GetMessageTypes()
        {
            return _scanned ? _messageOnlyTypes : new HashSet<string> { VerseMessage };
        }

        /// <summary>
        /// 双重过滤判定（参考上游 ArchivePatch.ShouldProcessArchivable）：
        /// 1. C# 类型名被禁用 -> 拒绝
        /// 2. Letter/Message 的 defName 被禁用 -> 拒绝
        /// 其余情况放行。只查本项目自建的 EnabledEventTypes，与 RimTalk 播报过滤解耦。
        /// </summary>
        public static bool ShouldCapture(IArchivable archivable, Dictionary<string, bool> enabledTypes)
        {
            if (archivable == null || enabledTypes == null) return false;

            string typeName = archivable.GetType().FullName;
            if (enabledTypes.TryGetValue(typeName, out var isTypeEnabled) && !isTypeEnabled)
                return false;

            string defName = GetDefName(archivable);
            if (defName != null &&
                enabledTypes.TryGetValue(defName, out var isDefEnabled) && !isDefEnabled)
                return false;

            return true;
        }

        /// <summary>获取 Letter/Message 的 defName（语言无关的事件细分标识）</summary>
        public static string GetDefName(IArchivable archivable)
        {
            if (archivable is Letter letter)
                return letter.def?.defName;
            if (archivable is Message message)
                return message.def?.defName;
            return null;
        }
    }
}
