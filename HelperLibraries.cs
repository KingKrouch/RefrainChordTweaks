using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
// Framerate Cap Stuff
using System.Runtime.InteropServices;
using System.Threading;
// SteamInput and Input Stuff
using Steamworks;
using RefrainChordTweaks;
using UnityEngine.InputSystem;

namespace KingKrouch.Utility.Helpers;

public class InputManager : MonoBehaviour
{
    public bool steamInputInitialized = false;
    public InputHandle_t[] inputHandles = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
    public InputHandle_t[] inputHandlesPrev = new InputHandle_t[Constants.STEAM_INPUT_MAX_COUNT];
    
    public void InitInput()
    {
        bool initialized = SteamManager.Initialized;
        ESteamInputType inputTypeP1 = ESteamInputType.k_ESteamInputType_Unknown;
        if (initialized)
        {
            if (!RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._bDisableSteamInput.Value) { steamInputInitialized = SteamInput.Init(false); }
            if (steamInputInitialized)
            {
                SteamInput.RunFrame();
                int result = SteamInput.GetConnectedControllers(inputHandles);
                inputHandlesPrev = inputHandles;
                // Grabs Player 1 Controller Type.
                inputTypeP1 = SteamInput.GetInputTypeForHandle(inputHandles[0]);
                switch (inputTypeP1)
                {
                    case ESteamInputType.k_ESteamInputType_Unknown:
                        // This is when the controller isn't detected at all. If SteamInput is disabled, it's going to return with this controller type.
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamController:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Xbox;
                        break;
                    case ESteamInputType.k_ESteamInputType_XBox360Controller:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Xbox;
                        break;
                    case ESteamInputType.k_ESteamInputType_XBoxOneController:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Xbox;
                        break;
                    case ESteamInputType.k_ESteamInputType_PS4Controller:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.PS4;
                        SteamInput.SetLEDColor(inputHandles[0], 252, 44, 3, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConPair:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Switch;
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchJoyConSingle:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Switch;
                        break;
                    case ESteamInputType.k_ESteamInputType_SwitchProController:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Switch;
                        break;
                    case ESteamInputType.k_ESteamInputType_PS3Controller: // TODO: Figure out why this won't get recognized.
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.PS4;
                        break;
                    case ESteamInputType.k_ESteamInputType_PS5Controller:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.PS5;
                        SteamInput.SetLEDColor(inputHandles[0], 252, 44, 3, 0);
                        break;
                    case ESteamInputType.k_ESteamInputType_SteamDeckController:
                        RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._confControllerType = RefrainChordTweaks.RefrainChordTweaks.EControllerType.Xbox;
                        break;
                    case ESteamInputType.k_ESteamInputType_Count:
                        break;
                    case ESteamInputType.k_ESteamInputType_MaximumPossibleValue:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Debug.Log("Connected Controller 1: " + inputTypeP1);
                //CreateNewPromptImages();
            }
            if (!steamInputInitialized || !RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._bDisableSteamInput.Value  || inputTypeP1 == ESteamInputType.k_ESteamInputType_Unknown)
            {
                // Put checks for native controllers here.
            }
            if (SteamUtils.IsSteamRunningOnSteamDeck()) {
                Debug.Log("Running on Steam Deck!"); // Should probably find a way to load optimized settings on first run.
            }
        }
    }

    private void Update()
    {
        if (SteamManager.Initialized && steamInputInitialized && !RefrainChordTweaks.RefrainChordTweaks.ConfigVariables._bDisableSteamInput.Value) {
            SteamInput.RunFrame();
            if (inputHandles != inputHandlesPrev) { // Checks if inputHandles is old, and if so, updates our inputHandles, and generates new prompt images for Player 1.
                int result = SteamInput.GetConnectedControllers(inputHandles);
                inputHandlesPrev = inputHandles;
                Debug.Log("Reconnected Controller 1: " + SteamInput.GetInputTypeForHandle(inputHandles[0]));
                //CreateNewPromptImages();
            }
        }
    }

    private void Start()
    { 
        InitInput();
    }
}

public class ResolutionManager : MonoBehaviour
{
    public bool enableDebug = false;
    public struct Resolution
    {
        // // Example Usage:
        // static Resolution resolutionNew = new Resolution(1920, 1080);
        // private int resolutionNewX      = resolutionNew.Width;
        public int Width { get; set; }
        public int Height { get; set; }

        public Resolution(int width, int height)
        {
            this.Width = width;
            this.Height = height;
        }
    }

    public static List<ResolutionManager.Resolution> ScreenResolutions()
    {
        var eResolutions = Screen.resolutions.Where(resolution => resolution.refreshRate == Screen.currentResolution.refreshRate); // Filter out any resolution that isn't supported by the current refresh rate.
        eResolutions.OrderBy(s => s); // Order by least to greatest.
        var aScreenResolutions = eResolutions as UnityEngine.Resolution[] ?? eResolutions.ToArray(); // Convert our Enumerable to an Array.
        var screenResolutions = new List<ResolutionManager.Resolution>(); // Creates the List we will be sorting resolutions in.
        for (int i = 0; i < aScreenResolutions.Length; i++) { // Run a for loop for each screen resolution in the array, since Unity's resolutions are incompatible with our own.
            var screenResolution = new ResolutionManager.Resolution(aScreenResolutions[i].width, aScreenResolutions[i].height);
            screenResolutions.Add(screenResolution);
        }

        // Our Hardcoded list of resolutions. We plan on appending these values to our resolution list only if the largest available display resolution is greater than one of these.
        var aHcResolutions = new ResolutionManager.Resolution[14];
        var hcResolutions   = new List<ResolutionManager.Resolution>();
        aHcResolutions[0].Width  = 640;   aHcResolutions[0].Height  = 360;
        aHcResolutions[1].Width  = 720;   aHcResolutions[1].Height  = 405;
        aHcResolutions[2].Width  = 800;   aHcResolutions[2].Height  = 450;
        aHcResolutions[3].Width  = 960;   aHcResolutions[3].Height  = 540;
        aHcResolutions[4].Width  = 1024;  aHcResolutions[4].Height  = 576;
        aHcResolutions[5].Width  = 1152;  aHcResolutions[5].Height  = 648;
        aHcResolutions[6].Width  = 1280;  aHcResolutions[6].Height  = 720;
        aHcResolutions[7].Width  = 1360;  aHcResolutions[7].Height  = 765;
        aHcResolutions[8].Width  = 1366;  aHcResolutions[8].Height  = 768;
        aHcResolutions[9].Width  = 1600;  aHcResolutions[9].Height  = 900;
        aHcResolutions[10].Width = 1920;  aHcResolutions[10].Height = 1080;
        aHcResolutions[11].Width = 2560;  aHcResolutions[11].Height = 1440;
        aHcResolutions[12].Width = 3840;  aHcResolutions[12].Height = 2160;
        aHcResolutions[13].Width = 7680;  aHcResolutions[13].Height = 4320;
        for (int i = 0; i < aHcResolutions.Length; i++) {
            hcResolutions.Add(aHcResolutions[i]);
        }
        int screenResolutionsCount = screenResolutions.Count - 1;
        for (int i = 0; i < hcResolutions.Count; i++) {
            if (screenResolutions[screenResolutionsCount].Width + screenResolutions[screenResolutionsCount].Height >
                hcResolutions[i].Width + hcResolutions[i].Height) {
                screenResolutions.Add(hcResolutions[i]);
            }
        }
        var resolutions = screenResolutions.Distinct().ToList();

        var resSort = from r in resolutions orderby r.Width + r.Height ascending select r;
        var resolutionsSorted   = new List<ResolutionManager.Resolution>();
        foreach (var r in resSort) {
            resolutionsSorted.Add(r);
        }
        return resolutionsSorted;
    }
    // Start is called before the first frame update
    void Start()
    {
        var sr = ResolutionManager.ScreenResolutions().ToList();

        for (int i = 0; i < sr.Count; i++) { // Now we will finally do what we want with the display resolution list.
            if (enableDebug) { Debug.Log(sr[i].Width + "x" + sr[i].Height); } // In this case, print a debug log to show we are doing things right.
        }
    }
}

public class FramerateLimiter : MonoBehaviour
{
    private FramerateLimiter m_Instance;
    public FramerateLimiter Instance { get { return m_Instance; } }
    public double fpsLimit  = 0.0f;

    [DllImport("Kernel32.dll", CallingConvention = CallingConvention.Winapi)]
    private static extern void GetSystemTimePreciseAsFileTime(out long filetime);
    
    private static long SystemTimePrecise()
    {
        long stp = 0;
        GetSystemTimePreciseAsFileTime(out stp);
        return stp;
    }
    
    private long _lastTime = SystemTimePrecise();

    void Awake()
    {
        m_Instance = this;
    }

    void OnDestroy()
    {
        m_Instance = null;
    }

    void Update()
    {
        if (fpsLimit == 0.0) return;
        _lastTime += TimeSpan.FromSeconds(1.0 / fpsLimit).Ticks;
        long now = SystemTimePrecise();

        if (now >= _lastTime)
        {
            _lastTime = now;
            return;
        }
        else
        {
            SpinWait.SpinUntil(() => { return (SystemTimePrecise() >= _lastTime); });
        }
    }
}