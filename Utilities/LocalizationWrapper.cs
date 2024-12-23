using PotionCraft.LocalizationSystem;


namespace PotionCraftAutoGarden.Utilities
{
    public class LocalizationWrapper
    {
        private static bool _isInitialized = false;

        public static void Init()
        {
            if (!_isInitialized)
            {
                LocalizationManager.OnInitialize.AddListener(SetModLocalization);
                _isInitialized = true;
            }
        }
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
            RegisterLoc("#mod_autogarden_fertilization_completed", "Auto-fertilization completed in this area", "本区域的自动施肥完成");
            RegisterLoc("#mod_autogarden_no_operable_crops", "No operable crops in this area", "本区域没有可以操作的作物");

            //RegisterLoc("#mod_autogarden_cost", "Cost", "成本");
            //RegisterLoc("#mod_autogarden_has", "Has", "已拥有");
            //RegisterLoc("#mod_autogarden_nothas", "<color=red>Items not owned, recommended</color>", "<color=red>未拥有，建议购入</color>");
        }
        //string localizedText = LocalizationManager.GetText("#your_mod_key_1");
    }

}
