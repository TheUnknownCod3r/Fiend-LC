using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using LethalLib.Modules;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine;
using UnityEngine.Assertions;
namespace TheFiend
{
    [BepInPlugin("com.TheFiend", "The Fiend", "1.1.1")]
    public class TheFiendPlugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("TheFiend");
        public static TheFiendPlugin instance;
        public static string RoleCompanyFolder = "Assets/TheFiend/";

        public static AssetBundle bundle;
        public static ManualLogSource logger;
        public static Config MyConfig { get; internal set; }

        private void Awake()
        {
            if (instance == null) instance = this;
            NetcodePatchAwake();
            logger = base.Logger;
            MyConfig = new Config(base.Config);
            bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(base.Info.Location), "thefiend"));
            EnemyType val = bundle.LoadAsset<EnemyType>(RoleCompanyFolder + "TheFiend.asset");
            Enemies.RegisterEnemy(val, TheFiend.Config.spawnChanceConfig, TheFiend.Config.Moon.Value, bundle.LoadAsset<TerminalNode>(RoleCompanyFolder + "TheFiendNode.asset"), bundle.LoadAsset<TerminalKeyword>(RoleCompanyFolder + "TheFiendKey.asset"));
            NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
            Utilities.FixMixerGroups(val.enemyPrefab);
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
            Item item = bundle.LoadAsset<Item>(RoleCompanyFolder + Name + ".asset");
            NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
            Utilities.FixMixerGroups(item.spawnPrefab);
            Items.RegisterScrap(item, Rare, Levels.LevelTypes.All);
        }
    }
}