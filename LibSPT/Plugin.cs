using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodCam.Patches;
using UnityEngine;

namespace HollywoodCam;

[BepInPlugin("com.janky.hollywoodcam", "Janky's Lights, Camera and Jank", HollywoodCamVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodCamVersion = "1.0.0";

    public static ManualLogSource Log;

    public static ConfigEntry<bool> ThirdPersonEnabled;
    public static ConfigEntry<KeyCode> ThirdPersonToggleKey;
    
    public static ConfigEntry<int> ThirdPersonFovChange;
    public static ConfigEntry<float> ThirdPersonFovSpeed;
    
    public static ConfigEntry<Vector3> CameraOffset;
    public static ConfigEntry<float> CameraAdsSpeed;

    public static ConfigEntry<bool> CrosshairEnabled;
    public static ConfigEntry<Color> CrosshairColor;
    public static ConfigEntry<float> CrosshairThickness;
    
    public static ConfigEntry<float> FlinchScale;
    
    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;
        
        SetupConfig();
        
        new GameWorldStartedPostfixPatch().Enable();
        new PlayerConstructorPostFixPatch().Enable();
        new PlayerShotReactionsPostFixPatch().Enable();
        
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
        const string headerCam = "1. Camera";
        const string headerCrosshair = "2. Crosshair";
        const string headerMisc = "3. Misc Flotsam";
        const string debug = "Debug";
        
        ThirdPersonEnabled = Config.Bind(headerCam, "Enable Third Person View", false, new ConfigDescription(
            "Toggles the third person view camera."
        ));
        ThirdPersonToggleKey = Config.Bind(headerCam, "Third Person Toggle Key", KeyCode.None, new ConfigDescription(
            "Set the key that will toggle between first and third person view."
        ));
        
        ThirdPersonFovChange = Config.Bind(headerCam, "Third Person FOV Change", 15, new ConfigDescription(
            "How much to increase or decrease the FOV when in third person view.",
            new AcceptableValueRange<int>(-50, 50)
        ));
        ThirdPersonFovSpeed = Config.Bind(headerCam, "Third Person FOV Speed", 0.33f, new ConfigDescription(
            "How fast to adjust the fov (in seconds).",
            new AcceptableValueRange<float>(0f, 3f)
        ));
        
        CameraOffset = Config.Bind(headerCam, "Camera offset", new Vector3(0.5f, 0.15f, -2f), new ConfigDescription(
            "The default camera offset when aiming without leaning or leaning right. Leaning left will flip the offset right to left."
        ));
        CameraAdsSpeed = Config.Bind(headerCam, "ADS Speed", 5.0f, new ConfigDescription(
            "How fast the camera moves from ADS to Third Person (in meters/second). No freedom units, sorry.",
            new AcceptableValueRange<float>(1, 100f)
        ));
        
        CrosshairEnabled = Config.Bind(headerCrosshair, "Enable Crosshair", true, new ConfigDescription(
            "Toggles the world space crosshair."
        ));
        CrosshairColor = Config.Bind(headerCrosshair, "Crosshair Color", Color.white, new ConfigDescription(
            "Color of the FOV circle."
        ));
        CrosshairThickness = Config.Bind(headerCrosshair, "Crosshair Thickness", 2.0f, new ConfigDescription(
            "Thickness of the FOV circle.",
            new AcceptableValueRange<float>(1f, 10f)
        ));

        FlinchScale = Config.Bind(headerMisc, "Flinch Scaling", 0.5f, new ConfigDescription(
            "How much flinch is applied when shot.",
            new AcceptableValueRange<float>(0f, 5f)
        ));
        
        _loggingEnabled = Config.Bind(debug, "Enable Debug Logging", true, new ConfigDescription(
            "Duh. Requires restarting the game to take effect."
        ));
    }
}