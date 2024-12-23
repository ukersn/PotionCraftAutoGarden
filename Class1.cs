using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Garden;
using UnityEngine;
using UnityEngine.InputSystem;
using BepInEx.Configuration;
using HarmonyLib;
using PotionCraft.ManagersSystem.Day;
using PotionCraft.InventorySystem;
using PotionCraft.ScriptableObjects;

using PotionCraft.LocalizationSystem;
using Key = UnityEngine.InputSystem.Key;
using PotionCraftAutoGarden.Utilities;

namespace PotionCraftAutoGarden
{
    [BepInPlugin("com.ukersn.plugin.AutoGarden", "PotionCraftAutoGarden", "1.1.1")]
    public class AutoGarden : BaseUnityPlugin
    {
        private static AutoGarden Instance;
        private bool isProcessing = false;
        private static ConfigEntry<bool> enableQuickOperations;
        private static ConfigEntry<Key> quickHarvestWaterHotkey;
        private static ConfigEntry<Key> quickFertilizeHotkey;
        private static ConfigEntry<bool> autoHarvestWaterOnWakeUp;
        private OperationHelper operationHelper_W; //自动浇水和收获的方法管理
        private OperationHelper operationHelper_F; //自动施肥的方法管理

        private List<KeyValuePair<InventoryItem, int>> wildGrowthPotions;
        private List<KeyValuePair<InventoryItem, int>> stoneSkinPotions;

        void Start()
        {
            LoggerWrapper.Init(Logger);
            LocalizationWrapper.Init();

            Logger.LogInfo("uk自动花园插件正在加载..");
            // 获取插件的配置
            var config = Config;

            // 创建启用/禁用快速操作（收割、浇水和施肥）的配置项
            enableQuickOperations = config.Bind("General",      // 配置分类
                                                "EnableQuickOperations",  // 配置键
                                                false,           // 默认值为 false
                                                "Enable quick automatic harvesting, watering, and fertilizing\n" +
                                                "(This may cause lag when used, but all operations will be completed instantly)\n" +
                                                "是否启用快速自动收割、浇水和施肥\n" +
                                                "(这样做可能在使用该功能时造成卡顿，但所有操作都会在一瞬间完成)");

            // 创建快捷键配置项，默认为 F1
            quickHarvestWaterHotkey = config.Bind("Hotkeys",
                                                  "AutoHarvestWaterKey",
                                                  Key.F1,
                                                  "Hotkey to activate automatic harvesting and watering\n" +
                                                  "用于启动自动收割浇水的热键");
            // 创建快捷键配置项，默认为 F2
            quickFertilizeHotkey = config.Bind("Hotkeys",
                                               "AutoFertilizeKey",
                                               Key.F2,
                                               "Hotkey to activate automatic fertilizing\n" +
                                               "用于启动自动施肥的热键");
            // 创建配置项：在起床后自动收割和浇水
            autoHarvestWaterOnWakeUp = config.Bind("General",
                                       "AutoHarvestWaterOnWakeUp",
                                       false,
                                       "Automatically harvest and water after waking up each day\n(Only takes effect from the second day after the tutorial ends)\n" +
                                       "在每天起床后启动自动收割和浇水\n(仅在教程结束后的第二天开始生效)");


            // 实例化 OperationHelper
            operationHelper_W = new OperationHelper(
                "#mod_autogarden_no_operable_crops",
                "#mod_autogarden_auto_harvest_watering_complete",
                "#mod_autogarden_insufficient_fertilizer_potion"
            );
            // 实例化 OperationHelper
            operationHelper_F = new OperationHelper(
                "#mod_autogarden_no_operable_crops",
                "#mod_autogarden_fertilization_completed",
                "#mod_autogarden_insufficient_fertilizer_potion"
            );

        }
        void Awake() {
            
            Harmony.CreateAndPatchAll(typeof(AutoGarden));
            Instance = this;
        }
        void Update()
        {
            if (Keyboard.current[quickHarvestWaterHotkey.Value].wasPressedThisFrame)//一键全自动收割浇水.
            {
                AutoWateringAndGether();
            }
            if (Keyboard.current[quickFertilizeHotkey.Value].wasPressedThisFrame) {
                AutoTryFertilize();
            }
        }

        
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(InventoryObject), "TakeFromInventory")]
        //static void AfterTakeFromInventory(InventoryObject __instance, int count, bool grab) {
        //    InventoryItem inventoryItem= CommonUtils.GetPropertyValueS<InventoryItem>(__instance, "InventoryItem");
        //    ItemsPanel itemsPanel = CommonUtils.GetPropertyValueS<ItemsPanel>(__instance, "ItemsPanel");
        //    Debug.Log(string.Format("拿起出物品 {0}，目前数量：{1}", count, itemsPanel.Inventory.GetItemCount(inventoryItem)));
        //}



        #region 自动施肥
        void AutoTryFertilize()
        {

            if (enableQuickOperations.Value)
            {
                operationHelper_F.ResetStatus();
                GameObject[] visibleSeeds = GameObjectHelper.GetVisibleSeeds();
                foreach (GameObject seed in visibleSeeds)
                {
                    if (!TryFertilize(seed))break;
                }
                operationHelper_F.ShowCompletedMessage();
            }
            else
            {
                StartCoroutine(ProcessTryFertilizeCoroutine());
            }
        }

        private IEnumerator ProcessTryFertilizeCoroutine()
        {
            operationHelper_F.ResetStatus();
            isProcessing = true;
            GameObject[] visibleSeeds = GameObjectHelper.GetVisibleSeeds();
            foreach (GameObject seed in visibleSeeds)
            {
                yield return StartCoroutine(TryFertilizeCoroutine(seed));
                if (!isProcessing)
                {
                    yield break;
                }
                // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                yield return new WaitForSeconds(0.05f);
            }
            isProcessing = false;
            operationHelper_F.ShowCompletedMessage();
        }
        private IEnumerator TryFertilizeCoroutine(GameObject seedObject)
        {
            GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
            if (growingSpotController == null)
            {
                Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
                yield break;
            }
            bool fertilizeResult = TryFertilize(seedObject);
            if (!fertilizeResult)
            {
                isProcessing = false;
                yield break;
            }

            yield return null; // 给游戏一帧的时间来处理这个操作
        }


        //private bool TryFertilizeNotRemove(GameObject seedObject)//对PotionCraft.ObjectBased.Garden.GrowingSpotController.TryFertilize()的重写（改写）
        //{
        //    PotionApplier potionApplier = null;
        //    do
        //    {
        //        GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
        //        if (growingSpotController == null) return false;
        //        if (growingSpotController.buildableItem.markedAsDestroyed)return false;
                
        //        GrowthHandler growthHandler = CommonUtils.GetPropertyValue<GrowthHandler>(growingSpotController, "GrowthHandler");
        //        potionApplier = CommonUtils.GetPropertyValue<PotionApplier>(growingSpotController, "PotionApplier");
        //        GrowingSpotScaler scaler = CommonUtils.GetPropertyValue<GrowingSpotScaler>(growingSpotController, "Scaler");
        //        if (growthHandler != null && potionApplier != null && scaler != null)
        //        {
        //            Growth growth = CommonUtils.GetPropertyValue<Growth>(growthHandler, "Growth");
        //            int growthValue = CommonUtils.GetPropertyValue<int>(growth, "Value"); //    int value = growingSpotController.GrowthHandler.Growth.Value;
        //            if (!TryApply(potionApplier, growthHandler))
        //            {
        //                Logger.LogInfo("施肥失败");
        //                return true;
        //            }
        //            growingSpotController.shouldMature = (growthHandler.IsGrown && growthValue < growthHandler.PhasesCount - 1);
        //            growingSpotController.visualObjectControllerExtender.PlayDissolveAnimation();
        //            scaler.AnimateFertilizing();
        //            growingSpotController.TrySpawnParticlesOnPlant();
        //            //无需原版代码删除背包里的物品 和设定高亮
        //            growingSpotController.TryMature(false);
        //            GatherAndWateringSeeds(seedObject); //结尾不管怎么样都对这个种子补充收获与浇水
        //        }
        //        else
        //        {
        //            Logger.LogInfo("有的属性没有找到");
        //            return false;
        //        }
        //    } while (potionApplier != null && potionApplier.ReadyToApply()); //对于没有成熟的作物，反复施肥和浇水
        //    return true;
        //}
        private bool TryFertilize(GameObject seedObject)//对PotionCraft.ObjectBased.Garden.GrowingSpotController.TryFertilize()的重写（改写）
        {
            Inventory inventory = GameObjectHelper.GetPlayInventory();
            (KeyValuePair<InventoryItem, int>[] WildGrowthPotions, KeyValuePair<InventoryItem, int>[] StoneSkinPotions) = GameObjectHelper.FindTargetPotionItems(inventory);
            wildGrowthPotions = new List<KeyValuePair<InventoryItem, int>>(WildGrowthPotions);
            stoneSkinPotions = new List<KeyValuePair<InventoryItem, int>>(StoneSkinPotions);
            PotionApplier potionApplier = null;
            do
            {
                GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
                if (growingSpotController == null)
                {
                    Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
                    return false;
                }
                if (growingSpotController.buildableItem.markedAsDestroyed)
                {
                    return false;
                }
                GrowthHandler growthHandler = CommonUtils.GetPropertyValue<GrowthHandler>(growingSpotController, "GrowthHandler");
                potionApplier = CommonUtils.GetPropertyValue<PotionApplier>(growingSpotController, "PotionApplier");
                GrowingSpotScaler scaler = CommonUtils.GetPropertyValue<GrowingSpotScaler>(growingSpotController, "Scaler");
                if (growthHandler != null && potionApplier != null && scaler != null)
                {

                    KeyValuePair<InventoryItem, int> potion = new KeyValuePair<InventoryItem, int>(null, 0);
                    if (growingSpotController.Ingredient.type == InventoryItemType.Crystal) potion = GameObjectHelper.PopPotion(stoneSkinPotions);
                    else if (growingSpotController.Ingredient.type == InventoryItemType.Herb || growingSpotController.Ingredient.type == InventoryItemType.Mushroom) potion = GameObjectHelper.PopPotion(wildGrowthPotions);

                    if (potion.Key == null || inventory.GetItemCount(potion.Key) <= 0)
                    {
                        operationHelper_F.ShowInterruptedMessage();
                        return false;
                    }

                    Growth growth = CommonUtils.GetPropertyValue<Growth>(growthHandler, "Growth");
                    int growthValue = CommonUtils.GetPropertyValue<int>(growth, "Value"); //    int value = growingSpotController.GrowthHandler.Growth.Value;


                    if (!TryApply(potionApplier, growthHandler))
                    {
                        return true;
                    }
                    //省略播放音效的代码
                    //Sound.Play(((Seed)this.buildableItem.InventoryItem).soundPreset.harvestSound, 1f, 1f, false, null, null);
                    //Sound.Play(Settings<SoundPresetInterface>.Asset.potionGrowthSound, 1f, 1f, false, null, null);
                    growingSpotController.shouldMature = (growthHandler.IsGrown && growthValue < growthHandler.PhasesCount - 1);
                    growingSpotController.visualObjectControllerExtender.PlayDissolveAnimation();
                    scaler.AnimateFertilizing();
                    growingSpotController.TrySpawnParticlesOnPlant();
                    //无需原版代码删除背包里的物品 和设定高亮
                    //((PotionItem)Managers.Cursor.grabbedInteractiveItem).DestroyItem();
                    inventory.RemoveItem(potion.Key, 1, true);
                    //Managers.Cursor.ReleasePanItem();
                    //growingSpotController.buildableItem.SetHovered(true);
                    growingSpotController.TryMature(false);
                    GatherAndWateringSeeds(seedObject); //结尾不管怎么样都对这个种子补充收获与浇水
                    operationHelper_F.IncrementCount(); //操作数统计 完成xx次施肥
                }
                else
                {
                    //Logger.LogInfo("有的属性没有找到");
                    return false;
                }

                //Logger.LogInfo(String.Format("施肥{0}", seedObject));
            } while (potionApplier != null && potionApplier.ReadyToApply()); //对于没有成熟的作物，反复施肥和浇水

            return true;
        }
        internal bool TryApply(PotionApplier potionApplier, GrowthHandler growthHandler)
        {//对PotionCraft.ObjectBased.Garden.PotionApplier.TryApply(GrowthHandler) 的重写，num的计算经过简化变成了以下的形式

            int num = !potionApplier.ReadyToApply() ? 0 : 3;
            if (num <= 0)
            {
                return false;
            }
            bool canFertilize = PotionApplier.CanFertilize(num);
            if (growthHandler.IsGrown)
            {
                potionApplier.growth.HasAppliedPotion = true;
            }
            growthHandler.AddGrowth(num, true, canFertilize);
            return true;
        }


        #endregion 自动施肥



        #region 自动收获与浇水


        [HarmonyPostfix]
        [HarmonyPatch(typeof(DayManager), "StartNewDay")]
        static void AfterWakeUp()
        {
            DayManager dayManager = GameObject.Find("Managers").GetComponent<DayManager>();
            if (autoHarvestWaterOnWakeUp.Value && dayManager.CurrentDayAbsoluteNum >= 2)
            {
                Instance.AutoWateringAndGether();
            }
        }
        void AutoWateringAndGether() {

            if (enableQuickOperations.Value)
            {
                operationHelper_W.ResetStatus();
                GameObject[] visibleSeeds = GameObjectHelper.GetAllSeeds();
                foreach (GameObject seed in visibleSeeds)
                {
                    GatherAndWateringSeeds(seed);
                }
                operationHelper_W.ShowCompletedMessage();
            }
            else
            {
                StartCoroutine(ProcessAllSeedsCoroutine());
            }

        }


        private IEnumerator ProcessAllSeedsCoroutine()
        {

            isProcessing = true;
            operationHelper_W.ResetStatus();
            //Logger.LogInfo("开始一键全自动收割浇水");

            GameObject[] visibleSeeds = GameObjectHelper.GetAllSeeds();

                foreach (GameObject seed in visibleSeeds)
                {
                    yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
                    // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                    yield return new WaitForSeconds(0.05f);
                }

            //Logger.LogInfo("一键全自动收割浇水完成");
            isProcessing = false;
            operationHelper_W.ShowCompletedMessage();
        }
        //private IEnumerator ProcessVisualSeedsCoroutine()
        //{
        //    isProcessing = true;
        //    //Logger.LogInfo("开始一键全自动收割浇水");

        //    GameObject[] visibleSeeds = GameObjectHelper.GetVisibleSeeds();
        //    foreach (GameObject seed in visibleSeeds)
        //    {
        //        yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
        //        // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
        //        yield return new WaitForSeconds(0.05f);
        //    }
        //    //Logger.LogInfo("一键全自动收割浇水完成");
        //    isProcessing = false;
        //}
        private IEnumerator GatherAndWateringSeedCoroutine(GameObject seedObject)
        {
            // 将原来的 GatherAndWateringSeeds 方法转换为协程
            GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
            if (growingSpotController == null)
            {
                Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
                yield break;
            }
            WateringHandler wateringHandler = growingSpotController.waterdropReceiver.wateringHandler;
            if (wateringHandler == null)
            {
                Logger.LogInfo(string.Format("Could not get wateringHandler for {0}", seedObject.name));
                yield break;
            }
            growingSpotController.plantGatherer.GatherIngredient();
            StartWatering(wateringHandler);
            yield return null; // 给游戏一帧的时间来处理这个操作
        }
        //void WateringPlants(GameObject plantObject)
        //{
        //    GrowingSpotController growingSpotController = plantObject.GetComponent<GrowingSpotController>();
        //    if (growingSpotController == null)
        //    {
        //        Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", plantObject.name));
        //    }
        //    WateringHandler wateringHandler = growingSpotController.waterdropReceiver.wateringHandler;
        //    if (wateringHandler == null)
        //    {
        //        Logger.LogInfo(string.Format("Could not get wateringHandler for {0}", plantObject.name));
        //    }
        //    StartWatering(wateringHandler);

        //}
        //void GatherSeeds(GameObject seedObject)
        //{

        //    GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
        //    if (growingSpotController == null)
        //    {
        //        Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
        //    }
        //    growingSpotController.plantGatherer.GatherIngredient();
        //}
        void GatherAndWateringSeeds(GameObject seedObject)
        {
            GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
            if (growingSpotController == null)
            {
                Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
            }
            WateringHandler wateringHandler = growingSpotController.waterdropReceiver.wateringHandler;
            if (wateringHandler == null)
            {
                Logger.LogInfo(string.Format("Could not get wateringHandler for {0}", seedObject.name));
            }
            growingSpotController.plantGatherer.GatherIngredient();
            StartWatering(wateringHandler);

        }

        internal void StartWatering(WateringHandler wateringHandler)
        {//这是游戏内WateringHandler的StartWatering方法的重写，具体修改的位置是num(浇水量)定义部分。
            if (wateringHandler.CanGrow)
            {
                return;
            }

            GrowthHandler growthHandler = CommonUtils.GetPropertyValue<GrowthHandler>(wateringHandler.growingSpot, "GrowthHandler");

            if (growthHandler != null)
            {
                bool isGrown = CommonUtils.GetPropertyValue<bool>(growthHandler, "IsGrown");
                bool canHarvest = CommonUtils.GetPropertyValue<bool>(growthHandler, "CanHarvest");
                if (canHarvest)
                {
                    return;
                }
                bool isFullyWatered = wateringHandler.IsFullyWatered;
                float num = 100f; // / WateringHandler.Settings.dropsUntilFull;
                wateringHandler.SetWateringPercentage(wateringHandler.growth.WateringPercentage + num);
                wateringHandler.UpdateValues(true);
                if (wateringHandler.IsFullyWatered && !isFullyWatered)
                {
                    ExperienceCategory experienceCategory = isGrown ? ExperienceCategory.WateringGrownPlant : ExperienceCategory.WateringUngrownPlant;
                    Managers.Room.plants.AddExperience(wateringHandler.growingSpot.Ingredient.GetItemType(), experienceCategory);
                }
                operationHelper_W.IncrementCount();
            }
            else
            {
                Logger.LogError("Failed to get GrowthHandler");
            }
        }
        #endregion 自动收获与浇水



    }
}
