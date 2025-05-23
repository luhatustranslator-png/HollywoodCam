using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodCam.Patches;
using UnityEngine;

namespace HollywoodCam;

public enum AdsModeEnum
{
    FirstPerson,
    Shoulder,
    None
}

public enum GunStanceSyncEnum
{
    Cam,
    Lean,
    None
}

[BepInPlugin("com.janky.hollywoodcam", "Janky's Lights, Camera and Jank", HollywoodCamVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodCamVersion = "1.0.0";

    public static ManualLogSource Log;

    public static ConfigEntry<bool> ThirdPersonEnabled;
    public static ConfigEntry<KeyCode> ThirdPersonToggleKey;

    public static ConfigEntry<Vector3> MainCameraOffset;
    public static ConfigEntry<int> MainCameraFovOffset;
    public static ConfigEntry<bool> ShoulderCameraEnabled;
    public static ConfigEntry<KeyCode> ShoulderCameraToggleKey;
    public static ConfigEntry<Vector3> ShoulderCameraOffset;
    public static ConfigEntry<int> ShoulderCameraFovOffset;
    public static ConfigEntry<float> CameraSwitchSpeed;

    public static ConfigEntry<AdsModeEnum> AdsModeBasic;
    public static ConfigEntry<AdsModeEnum> AdsModeOptic;
    
    public static ConfigEntry<bool> CrosshairEnabled;
    public static ConfigEntry<bool> CrosshairAdsOnlyEnabled;
    public static ConfigEntry<Color> CrosshairColor;
    public static ConfigEntry<float> CrosshairThickness;

    public static ConfigEntry<KeyCode> ShoulderSwapCameraKey;
    public static ConfigEntry<bool> ShoulderSwapCameraOnLeanEnabled;
    public static ConfigEntry<GunStanceSyncEnum> GunStanceSync;

    public static ConfigEntry<float> FovChangeSpeed;
    public static ConfigEntry<float> FlinchScale;

    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;

        SetupConfig();

        new GameWorldStartedPostfixPatch().Enable();
        new PlayerConstructorPostFixPatch().Enable();
        new PlayerShotReactionsPostFixPatch().Enable();
        new PlayerOnLeanPostfixPatch().Enable();

        if (_loggingEnabled.Value)
        {
            Log.LogInfo("Logging enabled");
        }
        else
        {
            Log.LogInfo("Logging disabled");
            BepInEx.Logging.Logger.Sources.Remove(Log);
        }
    }

    private void SetupConfig()
    {
        const string headerPerspective = "1. Perspective";
        const string headerCamera = "2. Camera";
        const string headerAiming = "3. Aiming";
        const string headerCrosshair = "4. Crosshair";
        const string headerShoulderSwap = "5. Stance";
        const string headerMisc = "6. Misc Flotsam";
        const string headerDebug = "7. Debug";

        ThirdPersonEnabled = Config.Bind(headerPerspective, "Enable Third Person View", true, new ConfigDescription(
            "Toggles the third person view camera.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        ThirdPersonToggleKey = Config.Bind(headerPerspective, "Third Person Toggle Key", KeyCode.None, new ConfigDescription(
            "Set the key that will toggle between first and third person view.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        
        MainCameraOffset = Config.Bind(headerCamera, "Main Cam Position Offset", new Vector3(0.5f, 0.15f, -1.5f), new ConfigDescription(
            "The default camera position offset.",
            tags: new ConfigurationManagerAttributes { Order = 7 }
        ));
        MainCameraFovOffset = Config.Bind(headerCamera, "Main Cam FOV Offset", 35, new ConfigDescription(
            "How much to increase or decrease the FOV when in the main camera mode. This is relative to the base game FOV." +
            "Ideally, the fov should increase slightly so that you have additional peripheral vision.",
            new AcceptableValueRange<int>(-50, 50),
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        ShoulderCameraEnabled = Config.Bind(headerCamera, "Enable Shoulder Cam", false, new ConfigDescription(
            "Switch to the shoulder camera instead of the main camera.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        ShoulderCameraToggleKey = Config.Bind(headerCamera, "Shoulder Cam Toggle Key", KeyCode.None, new ConfigDescription(
            "Toggles between the shoulder and main camera.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        ShoulderCameraOffset = Config.Bind(headerCamera, "Shoulder Cam Position Offset", new Vector3(0.5f, 0.15f, -0.5f), new ConfigDescription(
            "The shoulder camera position offset.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        ShoulderCameraFovOffset = Config.Bind(headerCamera, "Shoulder Cam FOV Offset", 0, new ConfigDescription(
            "How much to increase or decrease the FOV when in the shoulder camera mode. This is relative to the base game FOV." +
            "If you want the shoulder cam to always provide a bit of zoom, set this to a negative number." +
            "NB: ADS will separately zoom in, independently of this setting.",
            new AcceptableValueRange<int>(-50, 50),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        CameraSwitchSpeed = Config.Bind(headerCamera, "Camera Switch Speed", 5f, new ConfigDescription(
            "How fast the camera switches between offsets in m/s. Higher values are faster, lower values are smoother.",
            new AcceptableValueRange<float>(1, 25f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        AdsModeBasic = Config.Bind(headerAiming, "Basic Sight ADS Mode", AdsModeEnum.Shoulder, new ConfigDescription(
            "Determines the ADS logic for non-optic sights (this is iron, holo, reflex, etc...)."
        ));
        AdsModeOptic = Config.Bind(headerAiming, "Optic Sight ADS Mode", AdsModeEnum.FirstPerson, new ConfigDescription(
            "Determines the ADS logic for magnifying optic sights."
        ));

        CrosshairEnabled = Config.Bind(headerCrosshair, "Enable Crosshair", true, new ConfigDescription(
            "Toggles the world space crosshair in third person view.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        CrosshairAdsOnlyEnabled = Config.Bind(headerCrosshair, "Only During ADS", true, new ConfigDescription(
            "Show the crosshair only during ADS. It's a bit cheesy otherwise. Buy a laser sight you bum.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        CrosshairColor = Config.Bind(headerCrosshair, "Crosshair Color", Color.white, new ConfigDescription(
            "Color of the crosshair.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        CrosshairThickness = Config.Bind(headerCrosshair, "Crosshair Thiccness", 2f, new ConfigDescription(
            "Thiccness of the crosshair. Giggity",
            new AcceptableValueRange<float>(1f, 10f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        
        ShoulderSwapCameraKey = Config.Bind(headerShoulderSwap, "Shoulder Swap Cam Key", KeyCode.None, new ConfigDescription(
            "Key to swap the camera between shoulders.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        ShoulderSwapCameraOnLeanEnabled = Config.Bind(headerShoulderSwap, "Shoulder Swap Cam On Lean", true, new ConfigDescription(
            "Swap camera between shoulders based on lean direction.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        GunStanceSync = Config.Bind(headerShoulderSwap, "Gun Stance Sync", GunStanceSyncEnum.Cam, new ConfigDescription(
            "Sync the Gun Stance (left or right shoulder) to either the Camera Stance, Lean or nothing..",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        
        FovChangeSpeed = Config.Bind(headerMisc, "FOV Change Speed", 0.25f, new ConfigDescription(
            "How fast to adjust the fov (in seconds).",
            new AcceptableValueRange<float>(0f, 3f),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        FlinchScale = Config.Bind(headerMisc, "Flinch Amount", 0.1f, new ConfigDescription(
            "How much flinch is applied when shot. A small value goes a long way. Set to 5 if you feel like a bobble head.",
            new AcceptableValueRange<float>(0f, 5f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        _loggingEnabled = Config.Bind(headerDebug, "Enable Debug Logging", true, new ConfigDescription(
            "Duh. Requires restarting the game to take effect."
        ));
    }
}