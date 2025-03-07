# Potion Craft Auto Garden
This Mod is compatible with Potion Craft v2.0.1.2!

Tired of the daily routine of watering plants and harvesting crops? Auto Garden is here to help you complete all harvesting, watering, and fertilizing actions in your garden, and can automatically fertilize plants or crystals on the screen.

## Features:
> 1. One-key automatic harvesting and watering of plants, and automatic crystal harvesting
> * (Default key is set to F1. On laptops, you may need to use Fn+F1. You can set AutoHarvestWaterKey to another key in the configuration file)
> 
> 2. One-key automatic fertilizing of plants or crystals on the screen
> * (Default key is set to F2. On laptops, you may need to use Fn+F2. You can set AutoFertilizeKey to another key in the configuration file)
>
> 3. Automatic execution of plant harvesting, watering, and crystal harvesting at the start of each new day
> * (Default setting is false (off). You can set AutoHarvestWaterOnWakeUp to true in the configuration file to enable it)
> 
> 4. Adjustable speed for automatic plant harvesting, watering, crystal harvesting, and fertilizing
> * (Default setting is false (slow). You can set EnableQuickOperations to true in the configuration file to enable fast mode)
>
> 5. Switchable automatic fertilization range.
> * (Default setting is false, which only fertilizes plants or crystals on screen. You can set FertilizeAllSeeds to true in the configuration file to switch to fertilizing plants or crystals in all rooms)
>

# Installation Instructions
> * 1.Download and install [BepInEx_x64_5.4.22][0] from GitHub
> * 2.Extract PotionCraftAutoGarden.dll to the Potion Craft\BepInEx\plugins folder.



You can find the configuration file com.ukersn.plugin.AutoGarden.cfg in the directory "Potion Craft\BepInEx\config" after running the game for the first time to modify the settings.
> 
> Actually, I recommend using the following configuration. This way, every day when you wake up, it will automatically and quickly harvest and water plants, and harvest crystals:<br>
> 
> **EnableQuickOperations = true**<br>
> 
> **AutoHarvestWaterOnWakeUp = true**
> 

# My Other Projects
[Recipe Book Button Fix][1]: Used to fix a bug where, after playing the game for a long time, the clickable area of the recipe book button becomes larger, overlapping other buttons.

[Ukersn's Tweak Wizard][2]: Allows unrestricted planting of plants and crystals, and features to improve game FPS.

[Potion Craft Game Save Error Fixer/Editor][3] : Used to fix game save errors (corrupted saves)

-----


# 药剂工艺 自动花园
此Mod适配药剂工艺v2.0.1.2版本！

厌倦每天日常浇花和收菜的生活了么？自动花园来了，它可以帮您完成花园中所有的收获和浇水的动作，并且可以对屏幕内的植物或水晶进行自动施肥。

## 功能: 
> 1.一键自动收获浇水植物和自动收获水晶
> *  (默认设置为F1键触发，如果是笔记本电脑，可能需要使用Fn+F1键来触发，可以在配置文件中设定AutoHarvestWaterKey为其他键进行触发)
>
> 2.一键自动对屏幕内的植物或水晶进行施肥
> *  (默认设置为F2键触发，如果是笔记本电脑，可能需要使用Fn+F2键来触发，可以在配置文件中设定AutoFertilizeKey为其他键进行触发)
>
> 3.每天新的一天自动执行自动收获浇水植物和自动收获水晶
> *  (默认设置为false关闭，可以在配置文件中设定AutoHarvestWaterOnWakeUp为true来开启它)
>
> 4.可调节自动收获浇水植物、自动收获水晶、自动施肥的速度
> *  (默认设置为false缓慢，可以在配置文件中设定EnableQuickOperations为true来开启它)
>
> 5.可切换的自动施肥范围。
> *  (默认设置为false只对屏幕内的植物或水晶施肥，可以在配置文件中设定FertilizeAllSeeds为true来切换为对所有房间中的植物或水晶施肥)
> 


# 安装说明
> * 1. 在GitHub下载并安装 [BepInEx_x64_5.4.22][0]
> * 2. 将PotionCraftAutoGarden.dll解压到Potion Craft\BepInEx\plugins文件夹。



你可以在第一次运行游戏后在目录"Potion Craft\BepInEx\config"中找到配置文件com.ukersn.plugin.AutoGarden.cfg来修改配置
> 其实我更建议使用以下配置，这样的话每天起来就会自动且快速的自动收获浇水植物和自动收获水晶:<br>
> 
> **EnableQuickOperations = true**<br>
> 
> **AutoHarvestWaterOnWakeUp = true**
> 

# 我的其他项目
[配方书按钮修复][1] :用于修复游戏游玩久后配方书按钮可点击区域变大，从而覆盖其他按钮的bug

[Ukersn的游戏调整优化][2] : 允许植物和水晶无限制的种植，还有提升游戏FPS帧数的功能。

[Potion Craft 游戏存档错误修复器/编辑器][3]  ： 用于修复游戏存档错误(坏档)

[0]: https://github.com/BepInEx/BepInEx/releases
[1]: https://github.com/ukersn/PotionCraftOpenRecipeButtonFix
[2]: https://github.com/ukersn/Potion-Craft-Ukersn-s-TweakWizard
[3]: https://github.com/ukersn/Potion-Craft-Save-File-Error-Fixer-Editor