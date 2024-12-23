using PotionCraft.InventorySystem;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.ObjectBased.Garden;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace PotionCraftAutoGarden.Utilities
{
    public class GameObjectHelper
    {

        public static GameObject[] GetVisibleSeeds()
        {
            Camera mainCamera = Camera.main; // 获取主摄像机
            if (mainCamera == null)
            {
                LoggerWrapper.LogInfo("Main camera not found");
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
                    LoggerWrapper.LogError(string.Format("Could not find ItemSprite for {0}", seed.name));
                    return null;
                }
                // 获取 SpriteRenderer 组件
                SpriteRenderer spriteRenderer = itemSpriteTransform.GetComponent<SpriteRenderer>();
                if (spriteRenderer == null)
                {
                    LoggerWrapper.LogError("Could not find SpriteRenderer component on ItemSprite");
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
        public static GameObject[] GetAllSeeds()
        { //获得所有被种植的种子物体(比如水晶、植物)
            // 查找名为 "ItemContainer" 的对象
            GameObject itemContainer = GameObject.Find("ItemContainer");
            if (itemContainer == null)
            {
                LoggerWrapper.LogInfo("ItemContainer not found in the scene.");
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


        public static Inventory GetPlayInventory()
        {
            GameObject managers = GameObject.Find("Managers");
            if (managers == null)
            {
                LoggerWrapper.LogInfo("Managers not found in the scene.");
                return null;
            }
            PlayerManager playerManager = managers.GetComponent<PlayerManager>();
            if (playerManager == null)
            {
                LoggerWrapper.LogInfo("PlayerManager not found in the scene.");
                return null;
            }
            return playerManager.Inventory;
        }

        public static (KeyValuePair<InventoryItem, int>[] WildGrowthPotions, KeyValuePair<InventoryItem, int>[] StoneSkinPotions) FindTargetPotionItems(Inventory inventory)
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
                    foreach (PotionEffect effect in potion.effects)
                    {

                        if (effect.name.Equals("WildGrowth"))
                        {
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

        public static KeyValuePair<InventoryItem, int> PopPotion(List<KeyValuePair<InventoryItem, int>> potions)
        {
            //LoggerWrapper.LogInfo(String.Format("背包中药水数量：{0}", potions.Count));
            if (potions.Count == 0)
            {
                return new KeyValuePair<InventoryItem, int>(null, 0); // 或者抛出异常，取决于您如何处理空列表
            }
            var lastIndex = potions.Count - 1;
            var potion = potions[lastIndex];
            if(potion.Value <= 1) { 
                potions.RemoveAt(lastIndex);
            }
            return potion;

        }


    }
}
