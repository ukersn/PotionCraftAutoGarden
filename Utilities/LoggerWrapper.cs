using BepInEx.Logging;
namespace PotionCraftAutoGarden.Utilities
{
    public static class LoggerWrapper
    {
        private static ManualLogSource _logger;

        public static void Init(ManualLogSource logger)
        {
            _logger = logger;
        }

        public static void LogInfo(string message)
        {
            _logger?.LogInfo(message);
        }

        public static void LogError(string message)
        {
            _logger?.LogError(message);
        }

    }
    
}
