using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;
using static TootTally.Utils.Helpers.EasingHelper;
using UnityEngine;
using BepInEx.Logging;

namespace TootTally.WickedCursorMovement2
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    [BepInDependency("TootTally", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin, ITootTallyModule
    {
        public static Plugin Instance;

        private const string CONFIG_NAME = "WickedCursorMovement.cfg";
        private const string CONFIG_FIELD = "WickedCursorMovement";

        private const float DEFAULT_FREQ = 3.5f;
        private const float DEFAULT_DAMP = 0.12f;
        private const float DEFAULT_INIT = 1.5f;

        public Options option;
        public ConfigEntry<bool> ModuleConfigEnabled { get; set; }
        public bool IsConfigInitialized { get; set; }
        public string Name { get => PluginInfo.PLUGIN_NAME; set => Name = value; }

        public ManualLogSource GetLogger => Logger;

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
            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "WickedCursorMovement2", true, "TootTally Module version of WickedCursorMovement.");
            if (TootTally.Plugin.Instance.moduleSettings != null) OptionalTrombSettings.Add(TootTally.Plugin.Instance.moduleSettings, ModuleConfigEnabled);
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                Frequency = config.Bind(CONFIG_FIELD, "Frequency", DEFAULT_FREQ, "The strenght of the vibration (Higher vibrates more)."),
                Damping = config.Bind(CONFIG_FIELD, "Damping", DEFAULT_DAMP, "How fast the cursor settle at the original target (this value should be lower than 0, the close to 0 the more it will vibrate)."),
                InitialResponse = config.Bind(CONFIG_FIELD, "InitialResponse", DEFAULT_INIT, "How much it anticipates the motion (value higher than one will take time to accelerate, value lower than 0 will ancitipate the motion)."),
            };

            var settingsPage = OptionalTrombSettings.GetConfigPage("WickedCursorMovement");
            if (settingsPage != null) {
                OptionalTrombSettings.AddSlider(settingsPage, .1f, 9f, .01f, false, option.Frequency);
                OptionalTrombSettings.AddSlider(settingsPage, .1f, 9f, .01f, false, option.Damping);
                OptionalTrombSettings.AddSlider(settingsPage, .1f, 9f, .01f, false, option.InitialResponse);
            }

            Harmony.CreateAndPatchAll(typeof(WickedCursorMovementPatches), PluginInfo.PLUGIN_GUID);
            LogInfo($"Module loaded!");
        }

        public void UnloadModule()
        {
            Harmony.UnpatchID(PluginInfo.PLUGIN_GUID);
            LogInfo($"Module unloaded!");
        }

        public static class WickedCursorMovementPatches
        {
            public static SecondOrderDynamics cursorDynamics;
            public static Vector2 cursorPosition;
            public static Vector2 cursorDestination;

            [HarmonyPatch(typeof(HomeController), nameof(HomeController.Start))]
            [HarmonyPrefix]
            public static void Initialize()
            {
                cursorDynamics = new SecondOrderDynamics(Instance.option.Frequency.Value, Instance.option.Damping.Value, Instance.option.InitialResponse.Value);
            }


            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void GameControllerStartPostfixPatch(GameController __instance)
            {
                cursorPosition = __instance.pointer.transform.localPosition;
                cursorDynamics.SetStartVector(cursorPosition);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
            [HarmonyPrefix]
            public static void GameControllerUpdatePostfixPatch(GameController __instance)
            {
                cursorDestination = Input.mousePosition / 2.42f;
                cursorDestination.y -= 440 / 2;

                if (cursorDynamics != null && cursorPosition != cursorDestination)
                    cursorPosition.y = cursorDynamics.GetNewVector(cursorDestination, Time.deltaTime).y;
                __instance.pointer.transform.localPosition = cursorPosition;
            }
        }

        public class Options
        {
            public ConfigEntry<float> Frequency { get; set; }
            public ConfigEntry<float> Damping { get; set; }
            public ConfigEntry<float> InitialResponse { get; set; }
        }
    }
}