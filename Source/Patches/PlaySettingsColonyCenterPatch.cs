using HarmonyLib;
using Verse;
using RimWorld;
using RimTalkHealthEnhance.UI;

namespace RimTalkHealthEnhance.Patches
{
    /// <summary>
    /// 在 PlaySettings 右下角添加殖民地中心点按钮
    /// </summary>
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    public static class PlaySettingsColonyCenterPatch
    {
        static void Postfix(WidgetRow row, bool worldView)
        {
            ColonyCenterPlaySetting.DoPlaySettingsGlobalControls(row, worldView);
        }
    }
}
