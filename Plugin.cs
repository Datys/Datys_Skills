using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using ItemManager;
using JetBrains.Annotations;
using ServerSync;
using StatusEffectManager;
using UnityEngine;



namespace Datys_Skills
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class Datys_SkillsPlugin : BaseUnityPlugin
    {
        internal const string ModName = "Datys_Skills";
        internal const string ModVersion = "1.0.1";
        internal const string Author = "Datys";
        private const string ModGUID = Author + "." + ModName;
        private static string ConfigFileName = ModGUID + ".cfg";
        private static string ConfigFileFullPath = Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;
        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGUID);

        public static readonly ManualLogSource Datys_SkillsLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGUID)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        // Location Manager variables
        public Texture2D tex;
        private Sprite mySprite;
        private SpriteRenderer sr;
        
        public static ConfigEntry<KeyboardShortcut> ability1Key;
        public static ConfigEntry<KeyboardShortcut> ability2Key;
        
        public static ConfigEntry<float> ability1PosX;
        public static ConfigEntry<float> ability1PosY;
        public static ConfigEntry<float> ability2PosX;
        public static ConfigEntry<float> ability2PosY;
        
        public static bool AbilityActive = false;
        
        

        public enum Toggle
        {
            On = 1,
            Off = 0
        }

        public void Awake()
        {
            _serverConfigLocked = config("1 - General", "Lock Configuration", Toggle.On,
                "If on, the configuration is locked and can be changed by server admins only.");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);
            
            AbilityManager.Init();
            

            // Načti texturu ikony – cesta musí odpovídat umístění tvého assetu
            GameObject overlayObj = new GameObject("AbilityIconOverlay");
            overlayObj.AddComponent<AbilityIconOverlay>();
            DontDestroyOnLoad(overlayObj);

            // Nech overlay objekt persistovat přes scény
            
            ability1Key = Config.Bind(
                "Abilities", 
                "Ability 1 Key", 
                new KeyboardShortcut(KeyCode.F),
                "Key to trigger Ability 1."
            );

            ability2Key = Config.Bind(
                "Abilities", 
                "Ability 2 Key", 
                new KeyboardShortcut(KeyCode.G),
                "Key to trigger Ability 2."
            );
            
            ability1PosX = Config.Bind("Overlay", "AbilityFPosX", 50f, "X position for ability 1 icon.");
            ability1PosY = Config.Bind("Overlay", "AbilityFPosY", Screen.height - 200f, "Y position for ability 1 icon.");
            ability2PosX = Config.Bind("Overlay", "AbilityGPosX", 50f, "X position for ability 2 icon.");
            ability2PosY = Config.Bind("Overlay", "AbilityGPosY", Screen.height - 120f, "Y position for ability 2 icon.");

            
            //SFX
            GameObject D_sfx_sword_hit = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_sfx_sword_hit");
            CustomSE D_AbilityCooldown_1 = new("datys_skills", "D_AbilityCooldown_1"); 
            CustomSE D_Ability_Start = new("datys_skills", "D_Ability_Start"); 
            GameObject D_sfx_Start_Ability = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_sfx_Start_Ability");
            GameObject D_sfx_kromsword_swing = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_sfx_kromsword_swing");
            GameObject D_fx_block_camshake1 = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_fx_block_camshake1");
            GameObject D_fx_hit_camshake1 = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_fx_hit_camshake1");
            GameObject D_fx_swing_camshake1 = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_fx_swing_camshake1");
            GameObject D_sfx_meat_hit1 = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_sfx_meat_hit1");
            GameObject D_vfx_Meatblood1 = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_vfx_Meatblood1"); 
            GameObject D_VFX_Attack_Ground = ItemManager.PrefabManager.RegisterPrefab("datys_skills", "D_VFX_Attack_Ground");
            //Items
            Item D_Sword_Snake = new("datys_skills", "D_Sword_Snake");
            D_Sword_Snake.Configurable = Configurability.Disabled;
            
            Item D_Sword_Baldur = new("datys_skills", "D_Sword_Baldur");
            D_Sword_Baldur.Configurable = Configurability.Disabled;
            
            
            
            

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }
        
        
        private Texture2D LoadTexture(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                Debug.LogError("❌ Soubor s ikonou nebyl nalezen: " + filePath);
                return null;
            }
            byte[] fileData = System.IO.File.ReadAllBytes(filePath);
            Texture2D tex = new Texture2D(2, 2);
            if (!tex.LoadImage(fileData))
            {
                Debug.LogError("❌ Nepodařilo se načíst data z textury.");
                return null;
            }
            return tex;
        }

        private void OnDestroy()
        {
            Config.Save();
        }

        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                Datys_SkillsLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                Datys_SkillsLogger.LogError($"There was an issue loading your {ConfigFileName}");
                Datys_SkillsLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<Toggle> _serverConfigLocked = null!;

        private ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        private ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            [UsedImplicitly] public int? Order;
            [UsedImplicitly] public bool? Browsable;
            [UsedImplicitly] public string? Category;
            [UsedImplicitly] public Action<ConfigEntryBase>? CustomDrawer;
        }

        class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
}