using RimTalk;
using RimTalk.API;
using RimTalk.Service;
using RimTalkHealthEnhance.API;
using Verse;

namespace RimTalkHealthEnhance.API.Hooks
{
    /// <summary>
    /// 负责注册所有 Pawn 相关的 Hooks
    /// </summary>
    internal static class PawnHookRegistrar
    {
        /// <summary>
        /// 注册所有 Pawn Hooks
        /// </summary>
        public static void Register(string modId)
        {
            var settings = RimTalkHealthEnhanceMod.Settings;

            RegisterHealthHook(modId);
            RegisterEquipmentHook(modId);
            RegisterRelationsHook(modId, settings);
            RegisterTraitsHook(modId, settings);
            RegisterLocationHook(modId, settings);
        }

        /// <summary>
        /// Health Hook (Override) - 增强健康信息
        /// </summary>
        private static void RegisterHealthHook(string modId)
        {
            RimTalkPromptAPI.RegisterPawnHook(
                modId,
                ContextCategories.Pawn.Health,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeHealth) return null;

                    var infoLevel = PromptService.InfoLevel.Normal;
                    return HealthInfoBuilder.BuildEnhancedHealthContext(pawn, infoLevel);
                },
                priority: 50
            );
        }

        /// <summary>
        /// Equipment Hook (Override) - 增强装备信息
        /// </summary>
        private static void RegisterEquipmentHook(string modId)
        {
            RimTalkPromptAPI.RegisterPawnHook(
                modId,
                ContextCategories.Pawn.Equipment,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeEquipment) return null;

                    var infoLevel = PromptService.InfoLevel.Normal;
                    return EquipmentContextBuilder.Build(pawn, infoLevel);
                },
                priority: 50
            );
        }

        /// <summary>
        /// Relations Hook (Override) - 解除关系数量限制
        /// </summary>
        private static void RegisterRelationsHook(string modId, HealthEnhanceSettings settings)
        {
            RimTalkPromptAPI.RegisterPawnHook(
                modId,
                ContextCategories.Pawn.Social,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeRelations) return original;

                    if (!settings.UnlimitedRelations) return original;

                    return RelationsContextBuilder.GetRelationsStringUnlimited(pawn);
                },
                priority: 50
            );
        }

        /// <summary>
        /// Traits Hook (Override) - 解除特质数量限制
        /// </summary>
        private static void RegisterTraitsHook(string modId, HealthEnhanceSettings settings)
        {
            RimTalkPromptAPI.RegisterPawnHook(
                modId,
                ContextCategories.Pawn.Traits,
                ContextHookRegistry.HookOperation.Override,
                (pawn, original) =>
                {
                    var contextSettings = Settings.Get().Context;
                    if (!contextSettings.IncludeTraits) return original;

                    if (!settings.UnlimitedTraits) return original;

                    var infoLevel = PromptService.InfoLevel.Normal;
                    return TraitsContextBuilder.GetTraitsContextUnlimited(pawn, infoLevel);
                },
                priority: 50
            );
        }

        /// <summary>
        /// Location Hook (Append) - 追加相对位置信息
        /// </summary>
        private static void RegisterLocationHook(string modId, HealthEnhanceSettings settings)
        {
            RimTalkPromptAPI.RegisterPawnHook(
                modId,
                ContextCategories.Pawn.Location,
                ContextHookRegistry.HookOperation.Append,
                (pawn, original) =>
                {
                    if (!settings.ShowRelativeLocation) return original;
                    if (pawn?.Map == null) return original;

                    string relativeLocation = LocationContextBuilder.GetRelativeLocation(pawn);
                    if (!string.IsNullOrEmpty(relativeLocation))
                    {
                        string prefix = pawn.Map.IsPlayerHome
                            ? "Relative Position"
                            : "Current Map";
                        return original + $"\n{prefix}: {relativeLocation}";
                    }
                    return original;
                },
                priority: 50
            );
        }
    }
}