using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Ffft.Game;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using System;
using Ffft.Battle;
using Ffft.WorldMap;
using KingKrouch.Utility.Helpers;
using UnityEngine.Playables;

namespace RefrainChordTweaks
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class RefrainChordTweaks : BasePlugin
    {
        internal static new ManualLogSource Log;

        public enum EControllerType
        {
            PS4,
            PS5,
            Switch,
            Xbox
        }

        public struct ConfigVariables
        {
            // Aspect Ratio Config
            public static ConfigEntry<bool> _bOriginalUIAspectRatio; // On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.

            // Graphics Config
            public static ConfigEntry<int> _resolutionScale; // Goes from 25% to 200%. Then it's adjusted to a floating point value between 0.25-2.00x.
            public static ConfigEntry<int> _shadowCascades; // 0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)
            public static ConfigEntry<float> _fLodBias; // Default is 2.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons.
            public static ConfigEntry<int> _iForcedLodQuality; // Default is 0, goes up to LOD #3 without cutting insane amounts of level geometry.
            public static ConfigEntry<int> _iForcedTextureQuality; // Default is 0, goes up to 1/14th resolution.
            public static ConfigEntry<int> _anisotropicFiltering; // 0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF.
            public static ConfigEntry<bool> _bDepthOfField; // Quick Toggle for Post-Processing

            // Framelimiter Config
            public static ConfigEntry<int> _iFrameInterval; // "0" disables the framerate cap, "1" caps at your screen refresh rate, "2" caps at half refresh, "3" caps at 1/3rd refresh, "4" caps at quarter refresh.
            public static ConfigEntry<bool> _bvSync; // Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.

            // Input Config
            public static ConfigEntry<string> _sControllerType; // Xbox, PS4, PS5, Switch
            public static ConfigEntry<bool> _bDisableSteamInput; // For those that don't want to use SteamInput, absolutely hate it being forced, and would rather use Unity's built-in input system.
            public static EControllerType _confControllerType = EControllerType.Xbox;

            // Resolution Config
            public static ConfigEntry<bool> _bForceCustomResolution;
            public static ConfigEntry<int> _iHorizontalResolution;
            public static ConfigEntry<int> _iVerticalResolution;
        }
        
        public void LoadConfig()
        { 
            // Aspect Ratio Config
            ConfigVariables._bOriginalUIAspectRatio = Config.Bind("Resolution", "Original UI AspectRatio", false, "On: Presents UI aspect ratio at 16:9 screen space, Off: Spanned UI.");
            
            // Resolution Config
            ConfigVariables._bForceCustomResolution = Config.Bind("Resolution", "Force Custom Resolution", false, "Self Explanatory. A temporary toggle for custom resolutions until I can figure out how to go about removing the resolution count restrictions.");
            ConfigVariables._iHorizontalResolution = Config.Bind("Resolution", "Horizontal Resolution", 1280);
            ConfigVariables._iVerticalResolution = Config.Bind("Resolution", "Vertical Resolution", 720);

            // Graphics Config
            ConfigVariables._shadowCascades = Config.Bind("Graphics", "Shadow Cascades", 4, new ConfigDescription("0: No Shadows, 2: 2 Shadow Cascades, 4: 4 Shadow Cascades (Default)", new AcceptableValueRange<int>(0, 4)));
            ConfigVariables._fLodBias = Config.Bind("Graphics", "Draw Distance (Lod Bias)", (float)2.00, new ConfigDescription("Default is 2.00, but this can be adjusted for an increased or decreased draw distance. 4.00 is the max I'd personally recommend for performance reasons."));
            ConfigVariables._iForcedLodQuality = Config.Bind("Graphics", "LOD Quality", 0, new ConfigDescription("0: No Forced LODs (Default), 1: Forces LOD # 1, 2: Forces LOD # 2, 3: Forces LOD # 3. Higher the value, the less mesh detail.", new AcceptableValueRange<int>(0, 3)));
            ConfigVariables._iForcedTextureQuality = Config.Bind("Graphics", "Texture Quality", 0, new ConfigDescription("0: Full Resolution (Default), 1: Half-Res, 2: Quarter Res. Goes up to 1/14th res (14).", new AcceptableValueRange<int>(0, 14)));
            ConfigVariables._bDepthOfField = Config.Bind("Graphics", "Post-Processing", true, "On: Enables Post-Processing (Default), Off: Disables Post-Processing (Which may be handy for certain configurations)");
            ConfigVariables._anisotropicFiltering = Config.Bind("Graphics", "Anisotropic Filtering", 0, new ConfigDescription("0: Off, 2: 2xAF, 4: 4xAF, 8: 8xAF, 16: 16xAF", new AcceptableValueRange<int>(0, 16)));

            // Framelimiter Config
            ConfigVariables._iFrameInterval = Config.Bind("Framerate", "Framerate Cap Interval", 1, new ConfigDescription("0 disables the framerate limiter, 1 caps at your screen refresh rate, 2 caps at half refresh, 3 caps at 1/3rd refresh, 4 caps at quarter refresh.", new AcceptableValueRange<int>(0, 4)));
            ConfigVariables._bvSync = Config.Bind("Framerate", "VSync", true, "Self Explanatory. Prevents the game's framerate from going over the screen refresh rate, as that can cause screen tearing or increased energy consumption.");
            
            // Input Config
            ConfigVariables._sControllerType = Config.Bind("Input", "Controller Prompts Type", "Xbox", "Xbox, PS4, PS5, Switch (If SteamInput is enabled, 'Automatic' will be used regardless of settings)");
            if (!Enum.TryParse(ConfigVariables._sControllerType.Value, out ConfigVariables._confControllerType)) { 
                ConfigVariables._confControllerType = EControllerType.Xbox;
                Log.LogError($"Controller Type Value is invalid. Defaulting to Xbox.");
            }
            ConfigVariables._bDisableSteamInput = Config.Bind("Input", "Force Disable SteamInput", false, "Self Explanatory. Prevents SteamInput from ever running, forcefully, for those using DS4Windows/DualSenseX or wanting native controller support. Make sure to disable SteamInput in the controller section of the game's properties on Steam alongside this option.");
        }

        public void LoadGraphicsOptions()
        {
            QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
            Texture.SetGlobalAnisotropicFilteringLimits(ConfigVariables._anisotropicFiltering.Value, ConfigVariables._anisotropicFiltering.Value);
            Texture.masterTextureLimit           = ConfigVariables._iForcedTextureQuality.Value; // Can raise this to force lower the texture size. Goes up to 14.
            QualitySettings.maximumLODLevel      = ConfigVariables._iForcedLodQuality.Value; // Can raise this to force lower the LOD settings. 3 at max if you want it to look like a blockout level prototype.
            QualitySettings.lodBias              = ConfigVariables._fLodBias.Value;
            QualitySettings.shadowCascades       = ConfigVariables._shadowCascades.Value;
            
            // ConfigVariables._bDepthOfField.Value
        }

        public override void Load()
        {
            Log = base.Log;
            // Plugin startup logic
            Log.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");
            QualitySettings.vSyncCount  = 1;
            Application.targetFrameRate = 0;
            Time.fixedDeltaTime = 1.0f / Screen.currentResolution.refreshRate;
            LoadConfig();
            LoadGraphicsOptions();
            Harmony.CreateAndPatchAll(typeof(InputPatches));
            Harmony.CreateAndPatchAll(typeof(UIPatches));
            Harmony.CreateAndPatchAll(typeof(ResolutionPatches));
        }

        [HarmonyPatch]
        public class InputPatches
        {
            private static bool initSteamInputComponent = false;
            public static GameObject advInputMgrObject;
            public static InputManager advInputMgrComponent;
            
            //[HarmonyPatch(typeof(SteamManager), "Awake")]
            //[HarmonyPostfix]
            public static void SteamInputPatches(SteamManager __instance)
            {
                if (!initSteamInputComponent) {
                    advInputMgrObject = new GameObject {
                        name          = "AdvancedInputManager",
                        transform     = {
                            position  = __instance.gameObject.transform.position,
                            rotation  = __instance.gameObject.transform.rotation
                        }
                    };
                    advInputMgrComponent    = advInputMgrObject.AddComponent<InputManager>();
                    initSteamInputComponent = true;
                }
            }
        }
        
        [HarmonyPatch]
        public class UIPatches
        {
            [HarmonyPatch(typeof(CanvasScaler), "OnEnable")]
            [HarmonyPostfix]
            public static void CanvasScalerFixes(CanvasScaler __instance)
            {
                __instance.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            }

            public static Sprite ModifySpritePS4(CKeyBtnIcon.XboxButton Input)
            {
                switch (Input)
                {
                    case CKeyBtnIcon.XboxButton.None:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.None);
                    case CKeyBtnIcon.XboxButton.DirUp:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.DirUp);
                    case CKeyBtnIcon.XboxButton.DirDown:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.DirDown);
                    case CKeyBtnIcon.XboxButton.DirLeft:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.DirLeft);
                    case CKeyBtnIcon.XboxButton.DirRight:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.DirRight);
                    case CKeyBtnIcon.XboxButton.A:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Circle);
                    case CKeyBtnIcon.XboxButton.B:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Cross);
                    case CKeyBtnIcon.XboxButton.X:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Square);
                    case CKeyBtnIcon.XboxButton.Y:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Triangle);
                    case CKeyBtnIcon.XboxButton.LB:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.L1);
                    case CKeyBtnIcon.XboxButton.LT:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.L2);
                    case CKeyBtnIcon.XboxButton.LStickPush:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.L3);
                    case CKeyBtnIcon.XboxButton.LStick:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.LStick);
                    case CKeyBtnIcon.XboxButton.LStickUp:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.LStickUp);
                    case CKeyBtnIcon.XboxButton.LStickDown:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.LStickDown);
                    case CKeyBtnIcon.XboxButton.LStickLeft:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.LStickLeft);
                    case CKeyBtnIcon.XboxButton.LStickRight:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.LStickRight);
                    case CKeyBtnIcon.XboxButton.RB:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.R1);
                    case CKeyBtnIcon.XboxButton.RT:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.R2);
                    case CKeyBtnIcon.XboxButton.RStickPush:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.R3);
                    case CKeyBtnIcon.XboxButton.RStick:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.RStick);
                    case CKeyBtnIcon.XboxButton.RStickUp:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.RStickUp);
                    case CKeyBtnIcon.XboxButton.RStickDown:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.RStickDown);
                    case CKeyBtnIcon.XboxButton.RStickLeft:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.RStickLeft);
                    case CKeyBtnIcon.XboxButton.RStickRight:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.RStickRight);
                    case CKeyBtnIcon.XboxButton.Start:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Option);
                    case CKeyBtnIcon.XboxButton.Back:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Touch);
                    case CKeyBtnIcon.XboxButton.Dir_UD:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Dir_UD);
                    case CKeyBtnIcon.XboxButton.Dir_LR:
                        return CKeyBtnIcon.LoadIconPS4(CKeyBtnIcon.PS4Button.Dir_LR);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Input), Input, null);
                }
            }
            
            public static Sprite ModifySpritePS5(CKeyBtnIcon.XboxButton Input)
            {
                switch (Input)
                {
                    case CKeyBtnIcon.XboxButton.None:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.None);
                    case CKeyBtnIcon.XboxButton.DirUp:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.DirUp);
                    case CKeyBtnIcon.XboxButton.DirDown:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.DirDown);
                    case CKeyBtnIcon.XboxButton.DirLeft:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.DirLeft);
                    case CKeyBtnIcon.XboxButton.DirRight:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.DirRight);
                    case CKeyBtnIcon.XboxButton.A:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Circle);
                    case CKeyBtnIcon.XboxButton.B:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Cross);
                    case CKeyBtnIcon.XboxButton.X:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Square);
                    case CKeyBtnIcon.XboxButton.Y:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Triangle);
                    case CKeyBtnIcon.XboxButton.LB:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.L1);
                    case CKeyBtnIcon.XboxButton.LT:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.L2);
                    case CKeyBtnIcon.XboxButton.LStickPush:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.L3);
                    case CKeyBtnIcon.XboxButton.LStick:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.LStick);
                    case CKeyBtnIcon.XboxButton.LStickUp:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.LStickUp);
                    case CKeyBtnIcon.XboxButton.LStickDown:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.LStickDown);
                    case CKeyBtnIcon.XboxButton.LStickLeft:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.LStickLeft);
                    case CKeyBtnIcon.XboxButton.LStickRight:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.LStickRight);
                    case CKeyBtnIcon.XboxButton.RB:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.R1);
                    case CKeyBtnIcon.XboxButton.RT:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.R2);
                    case CKeyBtnIcon.XboxButton.RStickPush:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.R3);
                    case CKeyBtnIcon.XboxButton.RStick:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.RStick);
                    case CKeyBtnIcon.XboxButton.RStickUp:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.RStickUp);
                    case CKeyBtnIcon.XboxButton.RStickDown:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.RStickDown);
                    case CKeyBtnIcon.XboxButton.RStickLeft:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.RStickLeft);
                    case CKeyBtnIcon.XboxButton.RStickRight:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.RStickRight);
                    case CKeyBtnIcon.XboxButton.Start:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Option);
                    case CKeyBtnIcon.XboxButton.Back:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Touch);
                    case CKeyBtnIcon.XboxButton.Dir_UD:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Dir_UD);
                    case CKeyBtnIcon.XboxButton.Dir_LR:
                        return CKeyBtnIcon.LoadIconPS5(CKeyBtnIcon.PS5Button.Dir_LR);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Input), Input, null);
                }
            }
            
            public static Sprite ModifySpriteSwitch(CKeyBtnIcon.XboxButton Input) // For the time being, switch A/B and X/Y in SteamInput if you have Nintendo Layout enabled.
            {
                switch (Input)
                {
                    case CKeyBtnIcon.XboxButton.None:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.None);
                    case CKeyBtnIcon.XboxButton.DirUp:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.DirUp);
                    case CKeyBtnIcon.XboxButton.DirDown:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.DirDown);
                    case CKeyBtnIcon.XboxButton.DirLeft:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.DirLeft);
                    case CKeyBtnIcon.XboxButton.DirRight:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.DirRight);
                    case CKeyBtnIcon.XboxButton.A:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.B);
                    case CKeyBtnIcon.XboxButton.B:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.A);
                    case CKeyBtnIcon.XboxButton.X:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.Y);
                    case CKeyBtnIcon.XboxButton.Y:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.X);
                    case CKeyBtnIcon.XboxButton.LB:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.L);
                    case CKeyBtnIcon.XboxButton.LT:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.ZL);
                    case CKeyBtnIcon.XboxButton.LStickPush:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.SL);
                    case CKeyBtnIcon.XboxButton.LStick:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.LStick);
                    case CKeyBtnIcon.XboxButton.LStickUp:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.LStickUp);
                    case CKeyBtnIcon.XboxButton.LStickDown:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.LStickDown);
                    case CKeyBtnIcon.XboxButton.LStickLeft:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.LStickLeft);
                    case CKeyBtnIcon.XboxButton.LStickRight:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.LStickRight);
                    case CKeyBtnIcon.XboxButton.RB:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.R);
                    case CKeyBtnIcon.XboxButton.RT:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.ZR);
                    case CKeyBtnIcon.XboxButton.RStickPush:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.SR);
                    case CKeyBtnIcon.XboxButton.RStick:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.RStick);
                    case CKeyBtnIcon.XboxButton.RStickUp:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.RStickUp);
                    case CKeyBtnIcon.XboxButton.RStickDown:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.RStickDown);
                    case CKeyBtnIcon.XboxButton.RStickLeft:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.RStickLeft);
                    case CKeyBtnIcon.XboxButton.RStickRight:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.RStickRight);
                    case CKeyBtnIcon.XboxButton.Start:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.Plus);
                    case CKeyBtnIcon.XboxButton.Back:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.Minus);
                    case CKeyBtnIcon.XboxButton.Dir_UD:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.Dir_UD);
                    case CKeyBtnIcon.XboxButton.Dir_LR:
                        return CKeyBtnIcon.LoadIconSwitch(CKeyBtnIcon.SwitchButton.Dir_LR);
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Input), Input, null);
                }
            }

            [HarmonyPatch(typeof(CKeyBtnIcon), nameof(CKeyBtnIcon.LoadIconXbox), new Type[] { typeof(CKeyBtnIcon.XboxButton) })]
            [HarmonyPostfix]
            public static void ModifySprite(ref CKeyBtnIcon.XboxButton i_ButtonNo, ref Sprite __result)
            {
                switch (ConfigVariables._confControllerType)
                {
                    case EControllerType.PS4:
                        __result = ModifySpritePS4(i_ButtonNo);
                        return;
                    case EControllerType.PS5:
                        __result = ModifySpritePS5(i_ButtonNo);
                        return;
                    case EControllerType.Switch:
                        __result = ModifySpriteSwitch(i_ButtonNo);
                        return;
                    case EControllerType.Xbox:
                        return;
                    default:
                        return;
                }
            }
        }
        
        [HarmonyPatch]
        public class ResolutionPatches
        {
            [HarmonyPatch(typeof(CBtlCamera), nameof(CBtlCamera.Initialize))]
            [HarmonyPostfix]
            public static void FOVPatch(CBtlCamera __instance)
            {
                __instance.GetCamera().gateFit = Camera.GateFitMode.Overscan;
            }

            [HarmonyPatch(typeof(CScreenResolutions), nameof(CScreenResolutions.ChangeResolution))]
            [HarmonyPrefix]
            public static bool CustomResolutionOverride(CScreenResolutions __instance, ref CConfigData.ScreenMode i_ScreenMode, ref int i_RefreshRate)
            {
                FullScreenMode displayMode;
                if (ConfigVariables._bForceCustomResolution.Value)
                {
                    switch (i_ScreenMode) {
                        case CConfigData.ScreenMode.Window:
                            displayMode = FullScreenMode.Windowed;
                            break;
                        case CConfigData.ScreenMode.Borderess:
                            displayMode = FullScreenMode.FullScreenWindow;
                            break;
                        case CConfigData.ScreenMode.FullScreen:
                            displayMode = FullScreenMode.ExclusiveFullScreen;
                            displayMode = 0;
                            break;
                        default:
                            displayMode = FullScreenMode.Windowed;
                            break;
                    }
                    Screen.SetResolution(ConfigVariables._iHorizontalResolution.Value, ConfigVariables._iVerticalResolution.Value, displayMode, i_RefreshRate);
                    return false;
                }
                return true;
            }
        }
    }
}
