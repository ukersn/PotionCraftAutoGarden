using PotionCraft.ManagersSystem.Ingredient;
using PotionCraft.ManagersSystem.Player;
using PotionCraft.ManagersSystem.TMP;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.FloatingText;
using PotionCraft.Settings;
using System;
using System.Collections.Generic;
using BepInEx;
using UnityEngine;


namespace PotionCraftAutoGarden.Utilities
{
    public class Tooltis
    {

        public static void SpawnMessageText(string msg)
        {

            GameObject managers = GameObject.Find("Managers");
            if (managers == null)
            {
                LoggerWrapper.LogInfo("Managers not found in the scene.");
                return;
            }
            PlayerManager playerManager = managers.GetComponent<PlayerManager>();
            if (playerManager == null)
            {
                LoggerWrapper.LogInfo("PlayerManager not found in the scene.");
                return;
            }

            Vector2 a = Managers.Cursor.cursor.transform.position;
            Vector2 floatingTextCursorSpawnOffset = Vector2.zero;
            Vector2 vector = a + floatingTextCursorSpawnOffset;

            List<CollectedFloatingText.FloatingTextContent> list = new List<CollectedFloatingText.FloatingTextContent>();
            string commonAtlasName = Settings<TMPManagerSettings>.Asset.CommonAtlasName;
            CollectedFloatingText.FloatingTextContent item = new CollectedFloatingText.FloatingTextContent(string.Format("<voffset=0.085em><size=81%><sprite=\"{1}\" name=\"SpeechBubble ExclamationMark Icon\"></size>\u202f{0}", msg, commonAtlasName), CollectedFloatingText.FloatingTextContent.Type.Text, 0f);
            list.Add(item);

            Transform transform = Managers.Game.Cam.transform;
            CollectedFloatingText collectedFloatingText = CommonUtils.GetPropertyValueS<CollectedFloatingText>(Settings<IngredientManagerSettings>.Asset, "CollectedFloatingText");
            CollectedFloatingText.SpawnNewText(collectedFloatingText.gameObject, vector, list.ToArray(), transform, false, false);
        }


    }
}
