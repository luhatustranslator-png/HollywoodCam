using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodCam.Patches;
using UnityEngine;

namespace HollywoodCam;

public enum PointOfViewEnum
{
    FirstPerson,
    ThirdPerson
}

public enum CameraPositionEnum
{
    Main,
    Shoulder,
}

public enum CameraStanceEnum
{
    Left = -1,
    Right = 1
}

public enum GunStanceSyncEnum
{
    Cam,
    Lean,
    None
}

public enum AdsModeEnum
{
    FirstPerson,
    Shoulder,
    None
}

[BepInPlugin("com.janky.hollywoodcam", "Janky's Lights, Camera, Hodor", HollywoodCamVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string HollywoodCamVersion = "1.0.0";

    public static ManualLogSource Log;

    public static ConfigEntry<PointOfViewEnum> PointOfViewDefault;
    public static ConfigEntry<KeyCode> ThirdPersonToggleKey;

    public static ConfigEntry<CameraPositionEnum> CameraPositionDefault;
    public static ConfigEntry<Vector3> CameraMainOffset;
    public static ConfigEntry<Vector3> CameraShoulderOffset;
    public static ConfigEntry<KeyCode> CameraShoulderKey;
    public static ConfigEntry<float> CameraSpeed;

    public static ConfigEntry<AdsModeEnum> AdsModeOptic;
    public static ConfigEntry<AdsModeEnum> AdsModeBasic;
    public static ConfigEntry<int> AdsBasicFovChange;
    public static ConfigEntry<float> AdsFovChangeTime;

    public static ConfigEntry<bool> CrosshairEnabled;
    public static ConfigEntry<bool> CrosshairAdsOnlyEnabled;
    public static ConfigEntry<Color> CrosshairColor;
    public static ConfigEntry<float> CrosshairThickness;

    public static ConfigEntry<CameraStanceEnum> CameraStanceDefault;
    public static ConfigEntry<KeyCode> CameraStanceLeftKey;
    public static ConfigEntry<KeyCode> CameraStanceRightKey;
    public static ConfigEntry<bool> CameraStanceSwapOnLeanEnabled;
    public static ConfigEntry<GunStanceSyncEnum> GunStanceSync;

    public static ConfigEntry<float> SprintCameraSwitchSpeed;
    public static ConfigEntry<int> SprintFovChange;
    public static ConfigEntry<float> SprintFovChangeTime;
    public static ConfigEntry<Vector3> SprintOffsetFactor;
    
    public static ConfigEntry<float> FlinchScale;
    public static ConfigEntry<float> InteractionRange;

    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;

        SetupConfig();

        new GameWorldStartedPostfixPatch().Enable();
        new GameWorldDisposePostfixPatch().Enable();
        new PlayerConstructorPostFixPatch().Enable();
        new PlayerShotReactionsPostFixPatch().Enable();
        new PlayerOnLeanPostfixPatch().Enable();
        new PlayerBonesShiftWeaponRootPrefixPatch().Enable();
        
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
        const string headerStance = "5. Stance Control";
        const string headerSprint = "6. Sprint Camera";
        const string headerMisc = "7. Misc Flotsam";
        const string headerDebug = "8. Debug";

        PointOfViewDefault = Config.Bind(headerPerspective, "Default PoV", PointOfViewEnum.ThirdPerson, new ConfigDescription(
            "The default PoV to use at the start of the raid.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        ThirdPersonToggleKey = Config.Bind(headerPerspective, "3rd Person Toggle Key", KeyCode.None, new ConfigDescription(
            "Set the key that will toggle between first and third person view.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        CameraPositionDefault = Config.Bind(headerCamera, "Default Camera Position", CameraPositionEnum.Main, new ConfigDescription(
            "Determines the default camera position at the start of the raid, note that the game will dynamically adjust the actual position" +
            "based on multiple factors like visibility, ADS, etc.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        CameraMainOffset = Config.Bind(headerCamera, "Main Cam Offset", new Vector3(0.5f, 0.15f, -1.5f), new ConfigDescription(
            "The default camera position offset relative to the first person view (in meters).",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        CameraShoulderOffset = Config.Bind(headerCamera, "Shoulder Cam Offset", new Vector3(0.5f, 0.05f, -0.5f), new ConfigDescription(
            "The shoulder camera position offset relative to the first person view (in meters).",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        CameraShoulderKey = Config.Bind(headerCamera, "Shoulder Cam Key", KeyCode.None, new ConfigDescription(
            "Switches between the shoulder and main camera.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        CameraSpeed = Config.Bind(headerCamera, "Cam Switch Speed", 5f, new ConfigDescription(
            "How fast the camera switches between positions and stances in m/s. Higher values are faster, lower values are smoother.",
            new AcceptableValueRange<float>(1, 25f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        AdsModeOptic = Config.Bind(headerAiming, "Optic Sight ADS Mode", AdsModeEnum.FirstPerson, new ConfigDescription(
            "Determines the ADS logic for magnifying optic sights.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        AdsModeBasic = Config.Bind(headerAiming, "Basic Sight ADS Mode", AdsModeEnum.Shoulder, new ConfigDescription(
            "Determines the ADS logic for non-optic sights (this is iron, holo, reflex, etc...).",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        AdsBasicFovChange = Config.Bind(headerAiming, "3rd Person ADS FoV Change", -15, new ConfigDescription(
            "How much to change the FoV during ADS in third person. This is relative to the baseline FoV. Negative values will zoom in." +
            "The BSG default is -15.",
            new AcceptableValueRange<int>(-100, 100),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        AdsFovChangeTime = Config.Bind(headerAiming, "ADS FOV Change Time", 1f, new ConfigDescription(
            "The timespan (in seconds) that it takes to adjust the FOV for ADS. The BSG default is 1 second.",
            new AcceptableValueRange<float>(0f, 3f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
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


        CameraStanceDefault = Config.Bind(headerStance, "Default Camera Stance", CameraStanceEnum.Right, new ConfigDescription(
            "The default camera stance at the start of the raid. Note, the game might adjust the precise position on multiple factors like" +
            "visibility, ADS, etc.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        CameraStanceLeftKey = Config.Bind(headerStance, "Left Side Stance Key", KeyCode.Q, new ConfigDescription(
            "Switches the camera to the left side.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        CameraStanceRightKey = Config.Bind(headerStance, "Right Side Stance Key", KeyCode.E, new ConfigDescription(
            "Switches the camera to the right side.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        CameraStanceSwapOnLeanEnabled = Config.Bind(headerStance, "Stance Swap Cam On Lean", true, new ConfigDescription(
            "Swap the camera based on the lean direction. Note, there's no leaning during running, so you won't be able to always control" +
            "the stance based purely on the lean direction.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        GunStanceSync = Config.Bind(headerStance, "Gun Stance Sync", GunStanceSyncEnum.Cam, new ConfigDescription(
            "Sync the Gun Stance (left or right shoulder) to either the Camera Stance, Lean or nothing..",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        
        SprintCameraSwitchSpeed = Config.Bind(headerSprint, "Sprint Camera Switch Speed", 1.5f, new ConfigDescription(
            "How fast the camera switches to the sprint position and back.",
            new AcceptableValueRange<float>(0.5f, 25f),
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        SprintFovChange = Config.Bind(headerSprint, "Sprint FoV Change", 15, new ConfigDescription(
            "How much to change the FoV during sprinting in third person. This is relative to the baseline FoV. Positive numbers open up the FoV for" +
            "more peripheral vision. Negative numbers apply tunnel vision because you are a masochist.",
            new AcceptableValueRange<int>(-100, 100),
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        SprintFovChangeTime = Config.Bind(headerSprint, "Sprint FOV Change Time", 2f, new ConfigDescription(
            "The timespan (in seconds) that it takes to adjust the FOV for sprinting.",
            new AcceptableValueRange<float>(0f, 3f),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        SprintOffsetFactor = Config.Bind(headerSprint, "Sprint Cam Offset Factor", new Vector3(1f, 1.5f, 2.0f), new ConfigDescription(
            "Multiplies the current camera position offsets when sprinting. In practice, it's for moving the camera further back .",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        FlinchScale = Config.Bind(headerMisc, "Flinch Amount", 0.1f, new ConfigDescription(
            "How much flinch is applied when shot. A small value goes a long way. Set to 5 if you feel like a bobble head.",
            new AcceptableValueRange<float>(0f, 5f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        InteractionRange = Config.Bind(headerMisc, "3rd Person Interaction Range (RESTART)", 3f, new ConfigDescription(
            "How far away (in meters) you can interact with objects like doors or adult toy vending machines. This is measured from the camera" +
            "position, not the player position. If the camera is 2m behind the player, you need at least 3m to get reasonable interaction experience.",
            new AcceptableValueRange<float>(0f, 25f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        InteractionRange.SettingChanged += (s, e) =>
        {
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionRange.Value;
            EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = InteractionRange.Value;
            EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE = InteractionRange.Value;
        };
        
        EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionRange.Value;
        EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = InteractionRange.Value;
        EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE = InteractionRange.Value;
        
        _loggingEnabled = Config.Bind(headerDebug, "Enable Debug Logging", true, new ConfigDescription(
            "Duh. Requires restarting the game to take effect."
        ));
    }
}