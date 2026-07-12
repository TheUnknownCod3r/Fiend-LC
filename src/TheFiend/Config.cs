using System;
using BepInEx.Configuration;
using GameNetcodeStuff;
using HarmonyLib;
using LethalLib;
using LethalLib.Modules;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Bindings;
using UnityEngine.UIElements;

namespace TheFiend
{
    [Serializable]
    public class Config : SyncedInstance<Config>
    {
        public static ConfigEntry<int> SpawnChance;
        public static ConfigEntry<Levels.LevelTypes> Moon;

        public static ConfigEntry<int> FlickerRngChance;

        public static ConfigEntry<bool> WillRageAfterApparatus;

        public static ConfigEntry<float> Volume;

        public static int spawnChanceConfig;
        public static int FlickerRngConfig;
        public static bool WillRageAfterApparatusConfig;
        public static float VolumeConfig;
        public Config(ConfigFile cfg)
        {
            InitInstance(this);
            BindConfigs(cfg);
        }
        public void BindConfigs(ConfigFile cfg)
        {
            SpawnChance = cfg.Bind("Fiend", "Spawn Weight", 30, "The Chance for the Fiend to Spawn indoors");
            Moon = cfg.Bind("Fiend", "Moon", Levels.LevelTypes.All, "What Moon can the Fiend Spawn On?");
            FlickerRngChance = cfg.Bind("Fiend", "Flicker Chance", 1000, "This is a Random Chance out of 1/1000 happening to a random Player");
            WillRageAfterApparatus = cfg.Bind("Fiend", "Rage After Apparatus", true, "Trigger his rage mode if you remove the Apparatus.");
            Volume = cfg.Bind("Fiend", "Volume", 1f, "Sounds as scream and idle sound, not step sounds");
            spawnChanceConfig = SpawnChance.Value;
            FlickerRngConfig = FlickerRngChance.Value;
            WillRageAfterApparatusConfig = WillRageAfterApparatus.Value;
            VolumeConfig = Volume.Value;
        }

    }
}
