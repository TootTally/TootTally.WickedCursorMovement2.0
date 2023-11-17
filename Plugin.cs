using BaboonAPI.Hooks.Initializer;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.IO;
using TootTally.Utils;
using static TootTally.Utils.Helpers.EasingHelper;
using UnityEngine;
using BepInEx.Logging;
using TootTally.Utils.TootTallySettings;
using System.Configuration;

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
        public static TootTallySettingPage settingPage;

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
            ModuleConfigEnabled = TootTally.Plugin.Instance.Config.Bind("Modules", "WickedCursorMovementV2", true, "TootTally Module version of WickedCursorMovement.");
            TootTally.Plugin.AddModule(this);
        }

        public void LoadModule()
        {
            string configPath = Path.Combine(Paths.BepInExRootPath, "config/");
            ConfigFile config = new ConfigFile(configPath + CONFIG_NAME, true);
            option = new Options()
            {
                Frequency = config.Bind(CONFIG_FIELD, "Frequency", DEFAULT_FREQ, "The strenght of the vibration (Higher vibrates more)."),
                Damping = config.Bind(CONFIG_FIELD, "Damping", DEFAULT_DAMP, "How fast the cursor settle at the original target.\n(0 will vibrate forever, 100 will not vibrate)."),
                InitialResponse = config.Bind(CONFIG_FIELD, "InitialResponse", DEFAULT_INIT, "How much it anticipates the motion.\n(value higher than one will take time to accelerate, value lower than 0 will ancitipate the motion)."),
            };

            settingPage = TootTallySettingsManager.AddNewPage("WickedCursor\nMovementV2", "Wicked Cursor", 40f, new Color(0, 0, 0, 0));
            settingPage?.AddSlider("Frequency", .01f, 10f, option.Frequency, false);
            settingPage?.AddSlider("Damping", 0f, 10f, option.Damping, false);
            settingPage?.AddSlider("Initial Response", -10f, 10f, option.InitialResponse, false);
            settingPage.AddLabel($"Some good default values are:\nFreq: {DEFAULT_FREQ}\nDamp: {DEFAULT_DAMP}\nInit: {DEFAULT_INIT}");
            settingPage.AddLabel("To turn off the effect, go to the TootTally Module's page and turn off the module.");

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

            [HarmonyPatch(typeof(GameController), nameof(GameController.Start))]
            [HarmonyPostfix]
            public static void GameControllerStartPostfixPatch(GameController __instance)
            {
                cursorDynamics = new SecondOrderDynamics(Instance.option.Frequency.Value, Instance.option.Damping.Value, Instance.option.InitialResponse.Value);

                cursorPosition = __instance.pointer.transform.localPosition;
                cursorDynamics.SetStartVector(cursorPosition);
            }

            [HarmonyPatch(typeof(GameController), nameof(GameController.Update))]
            [HarmonyPrefix]
            public static void GameControllerUpdatePostfixPatch(GameController __instance)
            {
                cursorDestination = Input.mousePosition / 2.42f;
                cursorDestination.y -= 440f / 2f;

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