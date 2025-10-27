using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using LethalLib.Modules;
using UnityEngine;

namespace TheFiend;

[BepInPlugin("com.TheFiend", "The Fiend", "0.0.0")]
public class TheFiend : BaseUnityPlugin
{
    public static TheFiend instance;

    public static string RoleCompanyFolder = "Assets/TheFiend/";

    public static AssetBundle bundle;

    public static ConfigEntry<int> SpawnChance;

    public static ConfigEntry<Levels.LevelTypes> Moon;

    public static ConfigEntry<int> FlickerRngChance;

    public static ConfigEntry<bool> WillRageAfterApparatus;

    public static ConfigEntry<float> Volume;

    private void Awake()
    {
        ConfigFile configFile = new ConfigFile(Path.Combine(Paths.ConfigPath, "Fiend.cfg"), saveOnInit: true);
        SpawnChance = configFile.Bind("Fiend", "Spawn Weight", 30, new ConfigDescription("The Chance to spawn the fiend inside of the building", null));
        Moon = configFile.Bind("Fiend", "Moon", Levels.LevelTypes.All, new ConfigDescription("What is the only moon it can spawn on. Only one VALUE at a time.", null));
        FlickerRngChance = configFile.Bind("Fiend", "Flicker Chance", 1000, new ConfigDescription("This is a Random chance out of 1/1000 happening to a random player", null));
        WillRageAfterApparatus = configFile.Bind("Fiend", "Rage After Apparatus", defaultValue: true, new ConfigDescription("Trigger his rage mode if you remove the Apparatus.", null));
        Volume = configFile.Bind("Fiend", "Volume", 1f, new ConfigDescription("Sounds as scream and idle sound, not step sounds", null));
        Type[] types = Assembly.GetExecutingAssembly().GetTypes();
        Type[] array = types;
        foreach (Type type in array)
        {
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo[] array2 = methods;
            foreach (MethodInfo methodInfo in array2)
            {
                object[] customAttributes = methodInfo.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), inherit: false);
                if (customAttributes.Length != 0)
                {
                    methodInfo.Invoke(null, null);
                }
            }
        }
        instance = this;
        bundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(base.Info.Location), "thefiend"));
        EnemyType val = bundle.LoadAsset<EnemyType>(RoleCompanyFolder + "TheFiend.asset");
        Enemies.RegisterEnemy(val, SpawnChance.Value, Moon.Value, bundle.LoadAsset<TerminalNode>(RoleCompanyFolder + "TheFiendNode.asset"), bundle.LoadAsset<TerminalKeyword>(RoleCompanyFolder + "TheFiendKey.asset"));
        NetworkPrefabs.RegisterNetworkPrefab(val.enemyPrefab);
        Utilities.FixMixerGroups(val.enemyPrefab);
    }

    public void AddScrap(string Name, int Rare, Levels.LevelTypes level)
    {
        Item item = bundle.LoadAsset<Item>(RoleCompanyFolder + Name + ".asset");
        NetworkPrefabs.RegisterNetworkPrefab(item.spawnPrefab);
        Utilities.FixMixerGroups(item.spawnPrefab);
        Items.RegisterScrap(item, Rare, level);
    }
}
