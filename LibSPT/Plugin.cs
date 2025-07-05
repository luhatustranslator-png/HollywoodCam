using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HollywoodCam.GrenadeAssist;
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
    public const string HollywoodCamVersion = "1.1.0";

    public static ManualLogSource Log;

    public static ConfigEntry<PointOfViewEnum> PointOfViewDefault;
    public static ConfigEntry<KeyCode> ThirdPersonToggleKey;

    public static ConfigEntry<CameraPositionEnum> CameraPositionDefault;
    public static ConfigEntry<Vector3> CameraMainOffset;
    public static ConfigEntry<Vector3> CameraShoulderOffset;
    public static ConfigEntry<Vector3> CameraGrenadeOffset;
    public static ConfigEntry<KeyCode> CameraShoulderKey;
    public static ConfigEntry<float> CameraMoveSpeed;

    public static ConfigEntry<AdsModeEnum> AdsModeOptic;
    public static ConfigEntry<AdsModeEnum> AdsModeBasic;
    public static ConfigEntry<KeyCode> AdsModeSwapKey;
    public static ConfigEntry<float> AdsThirdPersonSensitivity;
    public static ConfigEntry<int> AdsBasicFovChange;
    public static ConfigEntry<float> AdsFovChangeTime;

    public static ConfigEntry<bool> CrosshairEnabled;
    public static ConfigEntry<bool> CrosshairAdsOnlyEnabled;
    public static ConfigEntry<Color> CrosshairColor;
    public static ConfigEntry<float> CrosshairThickness;

    public static ConfigEntry<CameraStanceEnum> CameraStanceDefault;
    public static ConfigEntry<KeyCode> CameraStanceToggleKey;
    public static ConfigEntry<KeyCode> CameraStanceLeftKey;
    public static ConfigEntry<KeyCode> CameraStanceRightKey;
    public static ConfigEntry<bool> CameraStanceSwapOnLeanEnabled;
    public static ConfigEntry<GunStanceSyncEnum> GunStanceSync;

    public static ConfigEntry<int> SprintFovChange;
    public static ConfigEntry<float> SprintFovChangeTime;

    public static ConfigEntry<float> InteractionRayRadius;
    public static ConfigEntry<float> FlinchScale;
    public static ConfigEntry<bool> ShimmyEnabled;
    public static ConfigEntry<float> InteractionRange;
    
    public static ConfigEntry<bool> GrenadeArcEnabled;
    public static ConfigEntry<float> GrenadeArcDistance;
    public static ConfigEntry<Color> GrenadeArcStartColor;
    public static ConfigEntry<Color> GrenadeArcEndColor;
    public static ConfigEntry<Color> GrenadeArcKnobColor;
    public static ConfigEntry<float> GrenadeArcKnobSize;
    public static ConfigEntry<float> GrenadeArcResolution;

    public static ConfigEntry<bool> DebugUIEnabled;
    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;

        SetupConfig();

        // Lifecycle
        new GameWorldStartedPostfixPatch().Enable();
        new PlayerDisposePrefixPatch().Enable();
        new PlayerOnDeadPrefixPatch().Enable();

        // UI
        new BattleUIOnShowAmmoPatch().Enable();
        new BattleUIOnShowFireModePatch().Enable();

        // Camera
        new PlayerPointOfViewPrefixPatch().Enable();
        new PlayerVisualPassPatch().Enable();
        new PlayerCameraControllerLateUpdatePrefixPatch().Enable();
        new ProceduralWeaponAnimationSetStrategyPrefixPatch().Enable();

        // Player
        new PlayerConstructorPostfixPatch().Enable();
        new PlayerShotReactionsPostfixPatch().Enable();

        // Lean
        new PlayerOnLeanPostfixPatch().Enable();

        // Sound
        new BaseSoundPlayerPointOfViewPrefixPatch().Enable();

        // Sensitivity
        new FirearmControllerUpdateSensitivityPrefixPatchPatch().Enable();

        // Interaction
        new GameWorldFindInteractablePrefixPatch().Enable();
        new PlayerInteractionRayPrefixPatch().Enable();

        // Grenade
        new GrenadeSetThrowForcePrefixPatch().Enable();
        new GrenadeAssistGameWorldStartedPostfixPatch().Enable();
        new GrenadeAssistPlayerDisposePrefixPatch().Enable();
        new GrenadeAssistPlayerOnDeadPrefixPatch().Enable();

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
        const string headerAiming = "2. Aiming";
        const string headerStance = "3. Stance Control";
        const string headerCamera = "4. Camera";
        const string headerCrosshair = "5. Crosshair";
        const string headerSprint = "6. Sprint Camera";
        const string headerMisc = "7. Misc Flotsam";
        const string headerAssist = "8. Assist";
        const string headerDebug = "9. Debug";

        PointOfViewDefault = Config.Bind(headerPerspective, "Default PoV", PointOfViewEnum.ThirdPerson, new ConfigDescription(
            "The default PoV to use at the start of the raid.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        ThirdPersonToggleKey = Config.Bind(headerPerspective, "3rd Person Toggle Key", KeyCode.None, new ConfigDescription(
            "Set the key that will toggle between first and third person view.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        AdsModeOptic = Config.Bind(headerAiming, "Optic Sight ADS Mode", AdsModeEnum.FirstPerson, new ConfigDescription(
            "Determines the ADS logic for magnifying optic sights.",
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        AdsModeBasic = Config.Bind(headerAiming, "Basic Sight ADS Mode", AdsModeEnum.Shoulder, new ConfigDescription(
            "Determines the ADS logic for non-optic sights (this is iron, holo, reflex, etc...).",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        AdsModeSwapKey = Config.Bind(headerAiming, "ADS Mode Swap Key", KeyCode.None, new ConfigDescription(
            "Switches between First Person and Shoulder Cam ADS for the currently equipped weapon.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        AdsThirdPersonSensitivity = Config.Bind(headerAiming, "ADS 3rd Person Sensitivity", 0.75f, new ConfigDescription(
            "Sensitivity multiplier for 3rd person ADS. The Live Tarkov default is 0.75.",
            new AcceptableValueRange<float>(0.1f, 5f),
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        AdsBasicFovChange = Config.Bind(headerAiming, "3rd Person ADS FoV Change", -15, new ConfigDescription(
            "How much to change the FoV during ADS in third person. This is relative to the baseline FoV. Negative values will zoom in. " +
            "The BSG default is -15, in other words, the FoV is reduced by 15 degrees.",
            new AcceptableValueRange<int>(-100, 100),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        AdsFovChangeTime = Config.Bind(headerAiming, "ADS FoV Change Time", 1f, new ConfigDescription(
            "The timespan (in seconds) that it takes to adjust the FoV for ADS. The BSG default is 1 second.",
            new AcceptableValueRange<float>(0f, 5f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        CameraStanceDefault = Config.Bind(headerStance, "Default Camera Stance", CameraStanceEnum.Right, new ConfigDescription(
            "The default camera stance at the start of the raid. Note, the game might adjust the precise position on multiple factors like " +
            "visibility, ADS, etc.",
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        CameraStanceToggleKey = Config.Bind(headerStance, "Toggle Stance Key", KeyCode.None, new ConfigDescription(
            "Toggles between each stance.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        CameraStanceLeftKey = Config.Bind(headerStance, "Left Side Stance Key", KeyCode.None, new ConfigDescription(
            "Switches the camera to the left side.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        CameraStanceRightKey = Config.Bind(headerStance, "Right Side Stance Key", KeyCode.None, new ConfigDescription(
            "Switches the camera to the right side.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        CameraStanceSwapOnLeanEnabled = Config.Bind(headerStance, "Stance Swap Cam On Lean", false, new ConfigDescription(
            "Swap the camera based on the lean direction. Note, there's no leaning during running, so you won't be able to always control " +
            "the stance based purely on the lean direction.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        GunStanceSync = Config.Bind(headerStance, "Gun Stance Sync", GunStanceSyncEnum.Cam, new ConfigDescription(
            "Automatically shoulder swaps the gun (left or right shoulder) based on either the Camera Stance, Lean direction or nothing.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        CameraPositionDefault = Config.Bind(headerCamera, "Default Camera Position", CameraPositionEnum.Main, new ConfigDescription(
            "Determines the default camera position at the start of the raid, note that the game will dynamically adjust the actual position " +
            "based on multiple factors like visibility, ADS, etc.",
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        CameraMainOffset = Config.Bind(headerCamera, "Main Cam Offset", new Vector3(0.5f, 0.15f, -1.5f), new ConfigDescription(
            "The default camera position offset relative to the first person view (in meters).",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        CameraShoulderOffset = Config.Bind(headerCamera, "Shoulder Cam Offset", new Vector3(0.5f, 0.05f, -0.5f), new ConfigDescription(
            "The shoulder camera position offset relative to the first person view (in meters).",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        CameraGrenadeOffset = Config.Bind(headerCamera, "Grenade Cam Offset", new Vector3(0.5f, 0.2f, -0.5f), new ConfigDescription(
            "The shoulder camera position offset relative to the first person view (in meters).",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        CameraShoulderKey = Config.Bind(headerCamera, "Shoulder Cam Key", KeyCode.None, new ConfigDescription(
            "Switches between the shoulder and main camera.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        CameraMoveSpeed = Config.Bind(headerCamera, "Cam Movement Speed", 1f, new ConfigDescription(
            "The speed with which the camera repositions itself when changing stances and avoiding obstacles. " +
            "Higher values will result in a twitchier camera.",
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
        CrosshairColor = Config.Bind(headerCrosshair, "Crosshair Color", Color.red, new ConfigDescription(
            "Color of the crosshair.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        CrosshairThickness = Config.Bind(headerCrosshair, "Crosshair Thiccness", 2f, new ConfigDescription(
            "Thiccness of the crosshair. Giggity",
            new AcceptableValueRange<float>(1f, 10f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        SprintFovChange = Config.Bind(headerSprint, "Sprint FoV Change", 15, new ConfigDescription(
            "How much to change the FoV during sprinting in third person. This is relative to the baseline FoV. Positive numbers open up the FoV for " +
            "more peripheral vision. Negative numbers apply tunnel vision because you are a masochist.",
            new AcceptableValueRange<int>(-100, 100),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        SprintFovChangeTime = Config.Bind(headerSprint, "Sprint FOV Change Time", 2f, new ConfigDescription(
            "The timespan (in seconds) that it takes to adjust the FOV for sprinting.",
            new AcceptableValueRange<float>(0f, 3f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));

        InteractionRayRadius = Config.Bind(headerMisc, "Interaction Radius", 0.1f, new ConfigDescription(
            "The radius of the raycast for picking interactable objects (in meters).",
            new AcceptableValueRange<float>(0.01f, 0.1f),
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        FlinchScale = Config.Bind(headerMisc, "Flinch Amount", 0.35f, new ConfigDescription(
            "How much flinch is applied when shot. A small value goes a long way. Set to 5 if you want to larp being a bobble head.",
            new AcceptableValueRange<float>(0f, 5f),
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        ShimmyEnabled = Config.Bind(headerMisc, "Shimmy When Turning", true, new ConfigDescription(
            "Toggles the feet shimmying when turning the torso. Enabled by default in Live Tarkov. Purely cosmetic.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        InteractionRange = Config.Bind(headerMisc, "3rd Person Interaction Range (RESTART)", 3.5f, new ConfigDescription(
            "How far away (in meters) you can interact with objects like doors or adult toy vending machines. This is measured from the camera " +
            "position, not the player position. If the camera is 2m behind the player, you need at least 3m to get reasonable interaction experience.",
            new AcceptableValueRange<float>(0f, 25f),
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
        InteractionRange.SettingChanged += (_, _) =>
        {
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionRange.Value;
            EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = InteractionRange.Value;
            EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE = InteractionRange.Value;
        };

        EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE = InteractionRange.Value;
        EFTHardSettings.Instance.DOOR_RAYCAST_DISTANCE = InteractionRange.Value;
        EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE = InteractionRange.Value;

        // Note: this is the overlap threshold where ADSing is prevented. Default is 0.
        EFTHardSettings.Instance.STOP_AIMING_AT = 100f;

        GrenadeArcEnabled = Config.Bind(headerAssist, "Enable Grenade Assist", true, new ConfigDescription(
            "Toggles a visual grenade assist - mostly for helping with 3rd person aiming.",
            tags: new ConfigurationManagerAttributes { Order = 7 }
        ));
        GrenadeArcDistance = Config.Bind(headerAssist, "Max Grenade Assist Distance", 25f, new ConfigDescription(
            "How far to draw the grenade assist arc.",
            new AcceptableValueRange<float>(1f, 50f),
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        GrenadeArcStartColor = Config.Bind(headerAssist, "Grenade Arc Start Color", new Color(0, 1, 0, 0f), new ConfigDescription(
            "Color of the start of the grenade arc. Make sure to configure the alpha value correctly so that you get a nice fade.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        GrenadeArcEndColor = Config.Bind(headerAssist, "Grenade Arc End Color", new Color(1, 0, 0, 0.75f), new ConfigDescription(
            "Color of the end of the grenade arc. Make sure to configure the alpha value correctly so that you get a nice fade.",
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        GrenadeArcKnobColor = Config.Bind(headerAssist, "Grenade Arc Knob Color", new Color(1, 0, 0, 0.75f), new ConfigDescription(
            "The uh, color of the knob. At the end of the big curved thing. Knob.",
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        GrenadeArcKnobSize = Config.Bind(headerAssist, "Grenade Arc Knob Size", 0.2f, new ConfigDescription(
            "Giggity.",
            new AcceptableValueRange<float>(0.01f, 1f),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        GrenadeArcResolution = Config.Bind(headerAssist, "Max Grenade Assist Resolution", 0.5f, new ConfigDescription(
            "Step size when calculating the grenade arc. Too small values take exponentially more time to compute and too large values become inaccurate.",
            new AcceptableValueRange<float>(0.1f, 2f),
            tags: new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true}
        ));

        DebugUIEnabled = Config.Bind(headerDebug, "Enable Debug UI", false, new ConfigDescription(
            "Enables the debug UI for diagnosing common issues.",
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        _loggingEnabled = Config.Bind(headerDebug, "Enable Debug Logging", false, new ConfigDescription(
            "Duh. Requires restarting the game to take effect.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
    }
}