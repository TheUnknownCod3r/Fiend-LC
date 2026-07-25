using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using System;
using System.Reflection;
using UnityEngine;
namespace TheFiend
{
    [BepInPlugin("com.TheFiend", "The Fiend", "1.1.6")]
    public class TheFiendPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("TheFiend");
        public static TheFiendPlugin instance;
        public static string RoleCompanyFolder = "Assets/TheFiend/";

        public static AssetBundle bundle;
        public static ManualLogSource logger;
        public static AssetBundle Assets;
        public static Config MyConfig { get; internal set; }
        internal Assembly assembly => Assembly.GetExecutingAssembly();
        internal string GetFilePath(string path)
        {
            return assembly.Location.Replace(assembly.GetName().Name + ".dll", path);
        }

        private void LoadAssets()
        {
            try
            {
                Assets = AssetBundle.LoadFromFile(GetFilePath("thefiend"));
            }
            catch (Exception arg)
            {
                logger.LogError($"Failed to load asset bundle! {arg}");
            }
        }
        private void Awake()
        {
            if (instance == null) instance = this;
            NetcodePatchAwake();
            LoadAssets();
            logger = base.Logger;
            MyConfig = new Config(base.Config);
            logger.LogInfo($"Loading {MyPluginInfo.PLUGIN_NAME}, Version v{MyPluginInfo.PLUGIN_VERSION}");
            EnemyType val = Assets.LoadAsset<EnemyType>("TheFiend.asset");
            TerminalNode val1 = Assets.LoadAsset<TerminalNode>("TheFiendNode.asset");
            TerminalKeyword val2 = Assets.LoadAsset<TerminalKeyword>("TheFiendKey.asset");
            NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
            Utilities.FixMixerGroups(val.enemyPrefab);
            Enemies.RegisterEnemy(val, TheFiend.Config.spawnChanceConfig, TheFiend.Config.Moon.Value,val1,val2);
            harmony.PatchAll(typeof(Plugin));
            harmony.PatchAll(typeof(TheFiendPlugin));
            harmony.PatchAll();
        }
        private static void NetcodePatchAwake()
        {
            // See https://github.com/EvaisaDev/UnityNetcodePatcher?tab=readme-ov-file#preparing-mods-for-patching
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }
        public void AddScrap(string Name, int Rare)
        {
            Item item = Assets.LoadAsset<Item>( Name + ".asset");
            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Utilities.FixMixerGroups(item.spawnPrefab);
            Items.RegisterScrap(item, Rare, Levels.LevelTypes.All);
        }
    }
}