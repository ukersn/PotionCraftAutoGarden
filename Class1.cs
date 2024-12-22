﻿using System;
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

namespace PotionCraftAutoGarden
{
    [BepInPlugin("com.ukersn.plugin.AutoGarden", "PotionCraftAutoGarden", "1.0.0")]
    public class AutoGarden : BaseUnityPlugin
    {
        private static AutoGarden Instance;
        private bool isProcessing = false;
        private static ConfigEntry<bool> enableQuickHarvestWater;
        private static ConfigEntry<Key> quickHarvestWaterHotkey;
        private static ConfigEntry<bool> autoHarvestWaterOnWakeUp;
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
            Logger.LogInfo("uk自动花园插件补丁正在加载..");

            Harmony.CreateAndPatchAll(typeof(AutoGarden));
            Instance = this;
        }
        void Update()
        {
            if (Keyboard.current[quickHarvestWaterHotkey.Value].wasPressedThisFrame)//一键全自动收割浇水.
            {
                AutoWateringAndGether();
            }

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

        void AutoWateringAndGether() {
            if (enableQuickHarvestWater.Value)
            {
                GameObject[] visibleSeeds = GetAllSeeds();
                foreach (GameObject seed in visibleSeeds)
                {
                    GatherAndWateringSeeds(seed);
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
            foreach (GameObject seed in visibleSeeds)
            {
                yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
                // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                yield return new WaitForSeconds(0.05f);
            }
            //Logger.LogInfo("一键全自动收割浇水完成");
            isProcessing = false;
        }
        private IEnumerator ProcessVisualSeedsCoroutine()
        {
            isProcessing = true;
            Logger.LogInfo("开始一键全自动收割浇水");

            GameObject[] visibleSeeds = GetVisibleSeeds();
            foreach (GameObject seed in visibleSeeds)
            {
                yield return StartCoroutine(GatherAndWateringSeedCoroutine(seed));
                // 可选：在每个种子处理后添加小延迟，以确保不会过度占用 CPU
                yield return new WaitForSeconds(0.05f);
            }
            Logger.LogInfo("一键全自动收割浇水完成");
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

    }
}
