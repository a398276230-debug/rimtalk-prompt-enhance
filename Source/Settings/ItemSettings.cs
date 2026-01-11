using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimTalkHealthEnhance
{
    /// <summary>
    /// 物品描述相关设置
    /// </summary>
    public class ItemSettings : IExposable
    {
        // === 装备与物品设置 ===
        public bool ShowEquipmentDesc = true;
        public bool ShowCarriedItemDesc = true;
        public bool ShowInventoryItems = false;
        public bool ShowInventoryDesc = false;
        public QualityCategory MinQualityForDesc = QualityCategory.Normal;
        public int ItemMaxDescriptionLength = 100;
        public int MaxInventoryItems = 3;
        public int MaxItemsWithDesc = 5;
        public bool SkipCommonItems = true;
        public bool SkipArtDescription = true;

        // === 交互设置 ===
        public bool ShowInteractionDesc = true;           // Show description of item/building being used
        public bool OnlyShowImportantBuildings = true;    // Only show important buildings (workbenches, etc.)
        public int InteractionMaxDescLength = 100;        // Max length for interaction description

        public void ExposeData()
        {
            Scribe_Values.Look(ref ShowEquipmentDesc, "showEquipmentDesc", true);
            Scribe_Values.Look(ref ShowCarriedItemDesc, "showCarriedItemDesc", true);
            Scribe_Values.Look(ref ShowInventoryItems, "showInventoryItems", false);
            Scribe_Values.Look(ref ShowInventoryDesc, "showInventoryDesc", false);
            Scribe_Values.Look(ref MinQualityForDesc, "minQualityForDesc", QualityCategory.Normal);
            Scribe_Values.Look(ref ItemMaxDescriptionLength, "itemMaxDescriptionLength", 100);
            Scribe_Values.Look(ref MaxInventoryItems, "maxInventoryItems", 3);
            Scribe_Values.Look(ref MaxItemsWithDesc, "maxItemsWithDesc", 5);
            Scribe_Values.Look(ref SkipCommonItems, "skipCommonItems", true);
            Scribe_Values.Look(ref SkipArtDescription, "skipArtDescription", true);
            
            Scribe_Values.Look(ref ShowInteractionDesc, "showInteractionDesc", true);
            Scribe_Values.Look(ref OnlyShowImportantBuildings, "onlyShowImportantBuildings", true);
            Scribe_Values.Look(ref InteractionMaxDescLength, "interactionMaxDescLength", 100);
        }

        /// <summary>
        /// 绘制物品设置 UI
        /// </summary>
        public void DrawSettings(Listing_Standard listing)
        {
            listing.CheckboxLabeled("RTE_Settings_Items_ShowEquipmentDesc".Translate(), ref ShowEquipmentDesc, "RTE_Settings_Items_ShowEquipmentDesc_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_ShowCarriedDesc".Translate(), ref ShowCarriedItemDesc, "RTE_Settings_Items_ShowCarriedDesc_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_ShowInventory".Translate(), ref ShowInventoryItems, "RTE_Settings_Items_ShowInventory_Desc".Translate());
            if (ShowInventoryItems)
            {
                listing.CheckboxLabeled("RTE_Settings_Items_ShowInventoryDesc".Translate(), ref ShowInventoryDesc, "RTE_Settings_Items_ShowInventoryDesc_Desc".Translate());
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();
            
            Text.Font = GameFont.Medium;
            Widgets.Label(listing.GetRect(30f), "RTE_Settings_Items_InteractionTitle".Translate());
            Text.Font = GameFont.Small;
            listing.Gap();
            
            listing.CheckboxLabeled("RTE_Settings_Items_ShowInteraction".Translate(), ref ShowInteractionDesc, "RTE_Settings_Items_ShowInteraction_Desc".Translate());
            if (ShowInteractionDesc)
            {
                listing.CheckboxLabeled("RTE_Settings_Items_OnlyImportantBuildings".Translate(), ref OnlyShowImportantBuildings, "RTE_Settings_Items_OnlyImportantBuildings_Desc".Translate());
                
                Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_InteractionMaxLength".Translate(InteractionMaxDescLength));
                InteractionMaxDescLength = (int)listing.Slider(InteractionMaxDescLength, 50, 200);
            }
            
            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Rect qualityRect = listing.GetRect(30f);
            Widgets.Label(qualityRect.LeftHalf(), "RTE_Settings_Items_MinQuality".Translate());
            if (Widgets.ButtonText(qualityRect.RightHalf(), MinQualityForDesc.GetLabel()))
            {
                List<FloatMenuOption> options = new List<FloatMenuOption>();
                foreach (QualityCategory quality in Enum.GetValues(typeof(QualityCategory)))
                {
                    options.Add(new FloatMenuOption(quality.GetLabel(), () => 
                    {
                        MinQualityForDesc = quality;
                    }));
                }
                Find.WindowStack.Add(new FloatMenu(options));
            }
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxDescLength".Translate(ItemMaxDescriptionLength));
            ItemMaxDescriptionLength = (int)listing.Slider(ItemMaxDescriptionLength, 50, 200);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxInventoryItems".Translate(MaxInventoryItems));
            MaxInventoryItems = (int)listing.Slider(MaxInventoryItems, 1, 10);
            listing.Gap(4f);

            Widgets.Label(listing.GetRect(22f), "RTE_Settings_Items_MaxItemsWithDesc".Translate(MaxItemsWithDesc));
            MaxItemsWithDesc = (int)listing.Slider(MaxItemsWithDesc, 1, 10);

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            listing.CheckboxLabeled("RTE_Settings_Items_SkipCommon".Translate(), ref SkipCommonItems, "RTE_Settings_Items_SkipCommon_Desc".Translate());
            listing.CheckboxLabeled("RTE_Settings_Items_SkipArt".Translate(), ref SkipArtDescription,
                "RTE_Settings_Items_SkipArt_Desc".Translate());

            listing.Gap();
            listing.GapLine();
            listing.Gap();

            Text.Font = GameFont.Tiny;
            Widgets.Label(listing.GetRect(40f), 
                "提示：品质等级从低到高为：Awful < Poor < Normal < Good < Excellent < Masterwork < Legendary\n" +
                "建议设置为Normal或Good以平衡信息量和token消耗");
            Text.Font = GameFont.Small;
        }
    }
}