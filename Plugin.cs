﻿using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;

namespace TootTally.ModuleTemplate
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTally", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "ModuleTemplate.cfg";
        private const string CONFIG_FIELD = "ModuleTemplate";
        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }
        public void LogInfo(string msg) => Logger.LogInfo(msg);
        public void LogError(string msg) => Logger.LogError(msg);

        private void Awake()
        {
            if (Instance != null) return;
            Instance = this;
            
            GameInitializationEvent.Register(Info, TryInitialize);
        }

        private void TryInitialize()
        {
            // Bind to the TTModules Config for TootTally
            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "ModuleTemplate", true, "<insert module description here>");
            // Attempt to add this module to the TTModules page in TrombSettings
            if (TootTally.Plugin.Instance.moduleSettings != null) OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                // Set your config here by binding them to the related ConfigEntry in your Options class
                // Example:
                // Unlimited = config.Bind(CONFIG_FIELD, "Unlimited", DEFAULT_UNLISETTING)
            };

            var settingsPage = OptionalTrombSettings.GetConfigPage("ModuleTemplate");
            if (settingsPage != null) {
                // Use OptionalTrombSettings to add your config to TrombSettings
                // Example:
                // OptionalTrombSettings.Add(settingsPage, option.Unlimited);
            }

            Harmony.CreateAndPatchAll(typeof(ModuleTemplatePatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

        public static class ModuleTemplatePatches
        {
            // Apply your Trombone Champ patches here
        }

        public class Options
        {
            // Fill this class up with ConfigEntry objects that define your configs
            // Example:
            // public ConfigEntry<bool> Unlimited { get; set; }
        }
    }
}