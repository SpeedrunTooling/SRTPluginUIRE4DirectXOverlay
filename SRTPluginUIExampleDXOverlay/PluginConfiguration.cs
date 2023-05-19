namespace SRTPluginUIRE4DirectXOverlay
{
    public class PluginConfiguration
    {
        public bool Debug { get; set; }
        // public bool ShowInventory { get; set; }
        public bool CenterPlayerHP { get; set; }
        public bool CenterBossHP { get; set; }
        public bool ShowHPBars { get; set; }
        public bool ShowDuffle { get; set; }
        public int EnemyLimit { get; set; }
        public bool ShowDamagedEnemiesOnly { get; set; }
        public bool ShowBossOnly { get; set; }
        public bool ShowDifficultyAdjustment { get; set; }
        public bool ShowPTAS { get; set; }
        public bool ShowPosition { get; set; }
        public bool ShowRotation { get; set; }
        // public bool ShowMapLocations { get; set; }
        public float FontSize { get; set; }
        public float ScalingFactor { get; set; }
        public float PositionX { get; set; }
        public float PositionY { get; set; }
    
        public float EnemyHPPositionX { get; set; }
        public float EnemyHPPositionY { get; set; }
    
        // public float InventoryPositionX { get; set; }
        // public float InventoryPositionY { get; set; }
    
        public string StringFontName { get; set; }
    
        public PluginConfiguration()
        {
            Debug = false;
            // ShowInventory = true;
            CenterPlayerHP = true;
            CenterBossHP = true;
            ShowDuffle = true;
            ShowHPBars = true;
            EnemyLimit = -1;
            ShowDamagedEnemiesOnly = false;
            ShowBossOnly = false;
            ShowDifficultyAdjustment = true;
            ShowPTAS = true;
            // ShowMapLocations = true;
            ShowPosition = true;
            ShowRotation = true;
            FontSize = 16f;
            ScalingFactor = 1f;
            PositionX = 5f;
            PositionY = 50f;
            EnemyHPPositionX = -1;
            EnemyHPPositionY = -1;
            // InventoryPositionX = -1;
            // InventoryPositionY = -1;
            StringFontName = "Courier New";
        }
    }
}