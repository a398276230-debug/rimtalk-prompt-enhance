using System;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 构建殖民地电力信息，用于发送给 AI 史官
    /// </summary>
    public static class PowerInfoBuilder
    {
        /// <summary>
        /// 构建电力信息上下文
        /// </summary>
        /// <returns>格式化的电力信息字符串，如果禁用则返回 null</returns>
        public static string BuildPowerContext()
        {
            var settings = RimTalkHealthEnhanceMod.Settings;
            if (!settings.IncludePowerInSnapshot)
                return null;
            
            var map = Find.CurrentMap;
            if (map == null)
                return null;
            
            var powerNetManager = map.powerNetManager;
            if (powerNetManager == null)
                return null;
            
            var allNets = powerNetManager.AllNetsListForReading;
            if (allNets == null || allNets.Count == 0)
                return null;
            
            // 汇总所有电网的统计
            float totalProduction = 0f;
            float totalConsumption = 0f;
            float totalBatteryStored = 0f;
            float totalBatteryCapacity = 0f;
            
            foreach (var powerNet in allNets)
            {
                if (powerNet == null)
                    continue;
                
                // 统计发电和耗电
                if (powerNet.powerComps != null)
                {
                    foreach (var comp in powerNet.powerComps)
                    {
                        var compTrader = comp as CompPowerTrader;
                        if (compTrader != null)
                        {
                            float output = compTrader.PowerOutput;
                            if (output > 0)
                                totalProduction += output;
                            else
                                totalConsumption += Math.Abs(output);
                        }
                    }
                }
                
                // 统计电池
                if (powerNet.batteryComps != null)
                {
                    foreach (var battery in powerNet.batteryComps)
                    {
                        if (battery != null)
                        {
                            totalBatteryStored += battery.StoredEnergy;
                            totalBatteryCapacity += battery.Props.storedEnergyMax;
                        }
                    }
                }
            }
            
            // 没有电力设施
            if (totalProduction == 0 && totalConsumption == 0 && totalBatteryCapacity == 0)
                return null;
            
            // 格式化输出
            var sb = new StringBuilder();
            float netPower = totalProduction - totalConsumption;
            
            sb.Append($"Power: {FormatWatts(totalProduction)} produced, {FormatWatts(totalConsumption)} consumed");
            
            if (netPower >= 0)
                sb.Append($" (surplus {FormatWatts(netPower)})");
            else
                sb.Append($" (deficit {FormatWatts(Math.Abs(netPower))})");
            
            if (totalBatteryCapacity > 0)
            {
                float percent = (totalBatteryStored / totalBatteryCapacity) * 100;
                sb.Append($". Batteries: {percent:F0}%");
            }
            
            return sb.ToString();
        }
        
        private static string FormatWatts(float watts)
        {
            if (Math.Abs(watts) >= 1000)
                return $"{watts / 1000:F1}kW";
            return $"{watts:F0}W";
        }
    }
}