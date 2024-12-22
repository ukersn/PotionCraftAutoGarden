using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Garden;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Reflection;
using BepInEx.Configuration;
using HarmonyLib;
using PotionCraft.ManagersSystem.Day;
using PotionCraft.InventorySystem;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.ManagersSystem.TMP;
using PotionCraft.ObjectBased.UIElements.FloatingText;

using PotionCraft.Settings;
using PotionCraft.LocalizationSystem;
using Key = UnityEngine.InputSystem.Key;

namespace PotionCraftAutoGarden
{
    [BepInPlugin("com.ukersn.plugin.AutoGarden", "PotionCraftAutoGarden", "1.0.0")]
    public class AutoGarden : BaseUnityPlugin
    {
        private static AutoGarden Instance;
        private bool isProcessing = false;
        private static ConfigEntry<bool> enableQuickHarvestWater;
        private static ConfigEntry<UnityEngine.InputSystem.Key> quickHarvestWaterHotkey;
        private static ConfigEntry<bool> autoHarvestWaterOnWakeUp;


        private List<KeyValuePair<InventoryItem, int>> wildGrowthPotions;
        private List<KeyValuePair<InventoryItem, int>> stoneSkinPotions;

        void Start()
        {
            Logger.LogInfo("uk自动花园插件正在加载..");
            // 获取插件的配置
            var config = Config;

            // 创建启用/禁用快速收割浇水的配置项
            enableQuickHarvestWater = config.Bind("General",      // 配置分类
                                                 "EnableQuickHarvestWater",  // 配置键
                                                 false,           // 默认值改为 false
                                                 "Enable quick automatic harvesting and watering\n(This may cause lag when used, but all harvesting and watering will be completed instantly)\n" +
                                                 "是否启用快速自动收割浇水\n(这样做可能使得在使用该功能的时候造成卡顿，但所有的收割和浇水都在一瞬间完成)");

            // 创建快捷键配置项，默认为 F1
            quickHarvestWaterHotkey = config.Bind("Hotkeys",
                                                  "AutoHarvestWaterKey",
                                                  Key.F1,
                                                  "Hotkey to activate automatic harvesting and watering\n" +
                                                  "用于启动自动收割浇水的热键");
            // 创建配置项：在起床后自动收割和浇水
            autoHarvestWaterOnWakeUp = config.Bind("General",
                                       "AutoHarvestWaterOnWakeUp",
                                       false,
                                       "Automatically harvest and water after waking up each day\n(Only takes effect from the second day after the tutorial ends)\n" +
                                       "在每天起床后启动自动收割和浇水\n(仅在教程结束后的第二天开始生效)");


        }
        void Awake() {
            LocalizationManager.OnInitialize.AddListener(SetModLocalization);
            Harmony.CreateAndPatchAll(typeof(AutoGarden));
            Instance = this;
        }
        void Update()
        {
            if (Keyboard.current[quickHarvestWaterHotkey.Value].wasPressedThisFrame)//一键全自动收割浇水.
            {
                AutoWateringAndGether();


            }
            if (Keyboard.current.f2Key.wasPressedThisFrame) {
                AutoTryFertilize();
            }
            //if (Keyboard.current.f3Key.wasPressedThisFrame)
            //{
            //    SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_insufficient_fertilizer_potion")); 
            //}

            //if (Keyboard.current.f2Key.wasPressedThisFrame)
            //{
            //    Logger.LogInfo("一键全自动收割浇水(无携程)");
            //    GameObject[] visibleSeeds = GetAllSeeds();
            //    foreach (GameObject seed in visibleSeeds)
            //    {
            //        GatherAndWateringSeeds(seed);
            //    }
            //}
            //if (Keyboard.current.f3Key.wasPressedThisFrame)
            //{
            //    Logger.LogInfo("浇水当前花园的东西.");
            //    GameObject[] visibleSeeds = GetVisibleSeeds();
            //    foreach (GameObject seed in visibleSeeds)
            //    {
            //        WateringPlants(seed);
            //    }
            //}
            //if (Keyboard.current.f4Key.wasPressedThisFrame)
            //{
            //}
        }

        
        //[HarmonyPostfix]
        //[HarmonyPatch(typeof(InventoryObject), "TakeFromInventory")]
        //static void AfterTakeFromInventory(InventoryObject __instance, int count, bool grab) {
        //    InventoryItem inventoryItem= GetPropertyValueS<InventoryItem>(__instance, "InventoryItem");
        //    ItemsPanel itemsPanel = GetPropertyValueS<ItemsPanel>(__instance, "ItemsPanel");
        //    Debug.Log(string.Format("拿起出物品 {0}，目前数量：{1}", count, itemsPanel.Inventory.GetItemCount(inventoryItem)));
        //}



        #region 自动施肥
        void AutoTryFertilize()
        {

            if (enableQuickHarvestWater.Value)
            {
                GameObject[] visibleSeeds = GetVisibleSeeds();
                if (visibleSeeds.Length == 0) SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_no_operable_crops"));
                foreach (GameObject seed in visibleSeeds)
                {
                    if (!TryFertilize(seed))break;
                }
            }
            else
            {
                StartCoroutine(ProcessTryFertilizeCoroutine());
            }
        }

        private IEnumerator ProcessTryFertilizeCoroutine()
        {
            isProcessing = true;
            GameObject[] visibleSeeds = GetVisibleSeeds();
            if (visibleSeeds.Length == 0) SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_no_operable_crops"));
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


        private bool TryFertilizeNotRemove(GameObject seedObject)//对PotionCraft.ObjectBased.Garden.GrowingSpotController.TryFertilize()的重写（改写）
        {
            PotionApplier potionApplier = null;
            do
            {
                GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
                if (growingSpotController == null) return false;
                if (growingSpotController.buildableItem.markedAsDestroyed)return false;
                
                GrowthHandler growthHandler = GetPropertyValue<GrowthHandler>(growingSpotController, "GrowthHandler");
                potionApplier = GetPropertyValue<PotionApplier>(growingSpotController, "PotionApplier");
                GrowingSpotScaler scaler = GetPropertyValue<GrowingSpotScaler>(growingSpotController, "Scaler");
                if (growthHandler != null && potionApplier != null && scaler != null)
                {
                    Growth growth = GetPropertyValue<Growth>(growthHandler, "Growth");
                    int growthValue = GetPropertyValue<int>(growth, "Value"); //    int value = growingSpotController.GrowthHandler.Growth.Value;
                    if (!TryApply(potionApplier, growthHandler))
                    {
                        Logger.LogInfo("施肥失败");
                        return true;
                    }
                    growingSpotController.shouldMature = (growthHandler.IsGrown && growthValue < growthHandler.PhasesCount - 1);
                    growingSpotController.visualObjectControllerExtender.PlayDissolveAnimation();
                    scaler.AnimateFertilizing();
                    growingSpotController.TrySpawnParticlesOnPlant();
                    //无需原版代码删除背包里的物品 和设定高亮
                    growingSpotController.TryMature(false);
                    GatherAndWateringSeeds(seedObject); //结尾不管怎么样都对这个种子补充收获与浇水
                }
                else
                {
                    Logger.LogInfo("有的属性没有找到");
                    return false;
                }
            } while (potionApplier != null && potionApplier.ReadyToApply()); //对于没有成熟的作物，反复施肥和浇水
            return true;
        }
        private bool TryFertilize(GameObject seedObject)//对PotionCraft.ObjectBased.Garden.GrowingSpotController.TryFertilize()的重写（改写）
        {
            Inventory inventory = GetPlayInventory();
            (KeyValuePair<InventoryItem, int>[] WildGrowthPotions, KeyValuePair<InventoryItem, int>[] StoneSkinPotions) = FindTargetPotionItems(inventory);
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
                GrowthHandler growthHandler = GetPropertyValue<GrowthHandler>(growingSpotController, "GrowthHandler");
                potionApplier = GetPropertyValue<PotionApplier>(growingSpotController, "PotionApplier");
                GrowingSpotScaler scaler = GetPropertyValue<GrowingSpotScaler>(growingSpotController, "Scaler");
                if (growthHandler != null && potionApplier != null && scaler != null)
                {

                    KeyValuePair<InventoryItem, int> potion = new KeyValuePair<InventoryItem, int>(null, 0);
                    if (growingSpotController.Ingredient.type == InventoryItemType.Crystal) potion = PopPotion(stoneSkinPotions);
                    else if (growingSpotController.Ingredient.type == InventoryItemType.Herb || growingSpotController.Ingredient.type == InventoryItemType.Mushroom) potion = PopPotion(wildGrowthPotions);

                    if (potion.Key == null || inventory.GetItemCount(potion.Key) <= 0)
                    {
                        SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_insufficient_fertilizer_potion"));
                        return false;
                    }

                    Growth growth = GetPropertyValue<Growth>(growthHandler, "Growth");
                    int growthValue = GetPropertyValue<int>(growth, "Value"); //    int value = growingSpotController.GrowthHandler.Growth.Value;


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
        void AutoWateringAndGether() {
            if (enableQuickHarvestWater.Value)
            {
                GameObject[] visibleSeeds = GetAllSeeds();
                if (visibleSeeds.Length == 0) SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_no_operable_crops"));
                else {
                    foreach (GameObject seed in visibleSeeds)
                    {
                        GatherAndWateringSeeds(seed);
                    }
                    SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_auto_harvest_watering_complete"));
                }


            }
            else
            {
                StartCoroutine(ProcessAllSeedsCoroutine());
            }
        }

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

        private IEnumerator ProcessAllSeedsCoroutine()
        {
            isProcessing = true;
            //Logger.LogInfo("开始一键全自动收割浇水");

            GameObject[] visibleSeeds = GetAllSeeds();
            if (visibleSeeds.Length == 0) SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_no_operable_crops"));
            else {
                foreach (GameObject seed in visibleSeeds)
                {
                    yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
                    // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                    yield return new WaitForSeconds(0.05f);
                }

                SpawnMessageText(LocalizationManager.GetText("#mod_autogarden_auto_harvest_watering_complete"));
            }

            //Logger.LogInfo("一键全自动收割浇水完成");
            isProcessing = false;
        }
        private IEnumerator ProcessVisualSeedsCoroutine()
        {
            isProcessing = true;
            //Logger.LogInfo("开始一键全自动收割浇水");

            GameObject[] visibleSeeds = GetVisibleSeeds();
            foreach (GameObject seed in visibleSeeds)
            {
                yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
                // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                yield return new WaitForSeconds(0.05f);
            }
            //Logger.LogInfo("一键全自动收割浇水完成");
            isProcessing = false;
        }
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
        void WateringPlants(GameObject plantObject)
        {
            GrowingSpotController growingSpotController = plantObject.GetComponent<GrowingSpotController>();
            if (growingSpotController == null)
            {
                Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", plantObject.name));
            }
            WateringHandler wateringHandler = growingSpotController.waterdropReceiver.wateringHandler;
            if (wateringHandler == null)
            {
                Logger.LogInfo(string.Format("Could not get wateringHandler for {0}", plantObject.name));
            }
            StartWatering(wateringHandler);

        }
        void GatherSeeds(GameObject seedObject)
        {

            GrowingSpotController growingSpotController = seedObject.GetComponent<GrowingSpotController>();
            if (growingSpotController == null)
            {
                Logger.LogInfo(string.Format("Could not get growingSpotController for {0}", seedObject.name));
            }
            growingSpotController.plantGatherer.GatherIngredient();
        }
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

            GrowthHandler growthHandler = GetPropertyValue<GrowthHandler>(wateringHandler.growingSpot, "GrowthHandler");

            if (growthHandler != null)
            {
                bool isGrown = GetPropertyValue<bool>(growthHandler, "IsGrown");
                bool canHarvest = GetPropertyValue<bool>(growthHandler, "CanHarvest");
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
            }
            else
            {
                Logger.LogError("Failed to get GrowthHandler");
            }
        }
        #endregion 自动收获与浇水
        #region 工具类
        // 通用反射方法来获取属性值
        private T GetPropertyValue<T>(object obj, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty)
        {
            if (obj == null)
            {
                Logger.LogError(string.Format("Object is null when trying to get property: {0}", propertyName));
                return default(T);
            }

            Type type = obj.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName, bindingFlags);

            if (propertyInfo != null)
            {
                try
                {
                    return (T)propertyInfo.GetValue(obj);
                }
                catch (Exception e)
                {
                    Logger.LogError(string.Format("Error getting property {0}: {1}", propertyName, e.Message));
                }
            }
            else
            {
                Logger.LogError(string.Format("Property not found: {0}", propertyName));
            }

            return default(T);
        }

        // 通用反射方法来获取属性值
        private static T GetPropertyValueS<T>(object obj, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.GetProperty)
        {
            if (obj == null)
            {
                Debug.Log(string.Format("Object is null when trying to get property: {0}", propertyName));
                return default(T);
            }

            Type type = obj.GetType();
            PropertyInfo propertyInfo = type.GetProperty(propertyName, bindingFlags);

            if (propertyInfo != null)
            {
                try
                {
                    return (T)propertyInfo.GetValue(obj);
                }
                catch (Exception e)
                {
                    Debug.Log(string.Format("Error getting property {0}: {1}", propertyName, e.Message));
                }
            }
            else
            {
                Debug.Log(string.Format("Property not found: {0}", propertyName));
            }

            return default(T);
        }


        GameObject[] GetVisibleSeeds()
        {
            Camera mainCamera = Camera.main; // 获取主摄像机
            if (mainCamera == null)
            {
                Logger.LogInfo("Main camera not found");
                return null;
            }
            GameObject[] seeds = GetAllSeeds();
            // 创建一个列表来存储找到的植物的对象
            List<GameObject> visiableSeeds = new List<GameObject>();

            foreach (GameObject seed in seeds)
            {
                if (seed == null)
                {
                    continue;
                }
                // 使用路径格式直接查找 ItemSprite
                Transform itemSpriteTransform = seed.transform.Find("Default GrowingSpot VisualObject/Visual Object/Backround/ItemSprite");
                if (itemSpriteTransform == null)
                {
                    Logger.LogError(string.Format("Could not find ItemSprite for {0}", seed.name));
                    return null;
                }
                // 获取 SpriteRenderer 组件
                SpriteRenderer spriteRenderer = itemSpriteTransform.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    Logger.LogError("Could not find SpriteRenderer component on ItemSprite");
                    return null;
                }
            // 检查 SpriteRenderer 是否在相机视野内
            if (GeometryUtility.TestPlanesAABB(GeometryUtility.CalculateFrustumPlanes(mainCamera), spriteRenderer.bounds))
                {
                    visiableSeeds.Add(seed.gameObject);
                }
            }

            return visiableSeeds.ToArray();

        }
        GameObject[] GetAllSeeds()
        { //获得所有被种植的种子物体(比如水晶、植物)
            // 查找名为 "ItemContainer" 的对象
            GameObject itemContainer = GameObject.Find("ItemContainer");
            if (itemContainer == null)
            {
                Logger.LogInfo("ItemContainer not found in the scene.");
                return new GameObject[0];
            }
            // 创建一个列表来存储找到的植物的对象
            List<GameObject> seeds = new List<GameObject>();

            // 遍历 ItemContainer 的所有子对象
            foreach (Transform child in itemContainer.transform)
            {
            // 检查子物体是否有 GrowingSpotController 组件且名称以 "Seed" 结尾
            if (child.GetComponent<GrowingSpotController>() != null && child.name.EndsWith("Seed"))
                {
                    seeds.Add(child.gameObject);
            }
            }
            return seeds.ToArray();
        }


        Inventory GetPlayInventory() {
            GameObject managers = GameObject.Find("Managers");
            if (managers == null)
            {
                Logger.LogInfo("Managers not found in the scene.");
                return null;
            }
            PlayerManager playerManager = managers.GetComponent<PlayerManager>();
            if (playerManager == null)
            {
                Logger.LogInfo("PlayerManager not found in the scene.");
                return null;
            }
            return playerManager.Inventory;
        }

        (KeyValuePair<InventoryItem, int>[] WildGrowthPotions, KeyValuePair<InventoryItem, int>[] StoneSkinPotions) FindTargetPotionItems(Inventory inventory)
        {
            InventoryItemIntDictionary items = inventory.items;
            List<KeyValuePair<InventoryItem, int>> wildGrowthResult = new List<KeyValuePair<InventoryItem, int>>();
            List<KeyValuePair<InventoryItem, int>> stoneSkinResult = new List<KeyValuePair<InventoryItem, int>>();
            // 遍历 ItemContainer 的所有子对象
            foreach (KeyValuePair<InventoryItem, int> item in items)
            {
                //item.Key 物体类型
                //item.Value 物体数量
                if (item.Key is Potion potion)
                {
                    int WildGrowthCount = 0;
                    int StoneSkinCount = 0;
                    foreach (PotionEffect effect in potion.effects) {
                        
                        if (effect.name.Equals("WildGrowth")) {
                            WildGrowthCount += 1;
                            continue;
                        }
                        if (effect.name.Equals("StoneSkin"))
                        {
                            StoneSkinCount += 1;
                            continue;
                        }
                    }
                    if (WildGrowthCount >= 3)
                    {
                        wildGrowthResult.Add(item);
                        continue;
                    }

                    if (StoneSkinCount >= 3)
                    {
                        stoneSkinResult.Add(item);
                        continue;
                    }

                }

            }
            return (wildGrowthResult.ToArray(), stoneSkinResult.ToArray());
        }

        public KeyValuePair<InventoryItem, int> PopPotion(List<KeyValuePair<InventoryItem, int>> potions)
        {
            if (potions.Count == 0)
            {
                return new KeyValuePair<InventoryItem, int> (null,0); // 或者抛出异常，取决于您如何处理空列表
            }

            var lastIndex = potions.Count - 1;
            var potion = potions[lastIndex];
            potions.RemoveAt(lastIndex);
            return potion;
        }
        #endregion 工具类

        #region 提示信息
        public void SpawnMessageText(string msg)
        {

            GameObject managers = GameObject.Find("Managers");
            if (managers == null)
            {
                Logger.LogInfo("Managers not found in the scene.");
                return ;
            }
            PlayerManager playerManager = managers.GetComponent<PlayerManager>();
            if (playerManager == null)
            {
                Logger.LogInfo("PlayerManager not found in the scene.");
                return ;
            }

            Vector2 a = Managers.Cursor.cursor.transform.position;
            Vector2 floatingTextCursorSpawnOffset = Vector2.zero;
            Vector2 vector = a + floatingTextCursorSpawnOffset;

            List<CollectedFloatingText.FloatingTextContent> list = new List<CollectedFloatingText.FloatingTextContent>();
            string commonAtlasName = Settings<TMPManagerSettings>.Asset.CommonAtlasName;
            CollectedFloatingText.FloatingTextContent item = new CollectedFloatingText.FloatingTextContent(string.Format("<voffset=0.085em><size=81%><sprite=\"{1}\" name=\"SpeechBubble ExclamationMark Icon\"></size>\u202f{0}", msg, commonAtlasName), CollectedFloatingText.FloatingTextContent.Type.Text, 0f);
            list.Add(item);
            
            Transform transform = Managers.Game.Cam.transform;
            CollectedFloatingText collectedFloatingText = GetPropertyValueS<CollectedFloatingText>(Settings<IngredientManagerSettings>.Asset, "CollectedFloatingText");
            CollectedFloatingText.SpawnNewText(collectedFloatingText.gameObject, vector, list.ToArray(), transform, false, false);
        }

        #endregion 提示信息








        #region Mod多语言

        public static void RegisterLoc(string key, string en, string zh)
        {
            for (int i = 0; i <= (int)LocalizationManager.Locale.cs; i++)
            {
                if ((LocalizationManager.Locale)i == LocalizationManager.Locale.zh)
                {
                    LocalizationManager.localizationData.Add(i, key, zh);
                }
                else
                {
                    LocalizationManager.localizationData.Add(i, key, en);
                }
            }
        }

        public static void SetModLocalization()
        {
            //RegisterLoc("#mod_autogarden_value", "Value", "价值"); 
            RegisterLoc("#mod_autogarden_insufficient_fertilizer_potion", "<color=red>Insufficient potion for fertilization</color>", "<color=red>用于施肥的药水不足</color>");
            RegisterLoc("#mod_autogarden_auto_harvest_watering_complete", "Auto-harvest and watering completed", "自动收获浇水完成");
            RegisterLoc("#mod_autogarden_no_operable_crops", "No operable crops in this area", "本区域没有可以操作的作物");
            //RegisterLoc("#mod_autogarden_cost", "Cost", "成本");
            //RegisterLoc("#mod_autogarden_has", "Has", "已拥有");
            //RegisterLoc("#mod_autogarden_nothas", "<color=red>Items not owned, recommended</color>", "<color=red>未拥有，建议购入</color>");
        }
        //string localizedText = LocalizationManager.GetText("#your_mod_key_1");
        #endregion Mod多语言

    }
}
