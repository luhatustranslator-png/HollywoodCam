using Comfort.Common;
using EFT;
using EFT.CameraControl;
using HarmonyLib;
using HollywoodCam.Collision;
using HollywoodCam.Helpers;
using UnityEngine;

namespace HollywoodCam;

public static class CameraExtensions
{
    public static Vector2 WorldPointToVisibleScreenPoint(this Camera camera, Vector3 worldPoint)
    {
        var screenPoint = camera.WorldToScreenPoint(worldPoint);
        var scale = Screen.height / (float)camera.scaledPixelHeight;
        screenPoint.y = Screen.height - screenPoint.y * scale;
        screenPoint.x *= scale;
        if (screenPoint is { z: > 0.01f, x: > -5f, y: > -5f } && screenPoint.x < Screen.width && screenPoint.y < Screen.height)
            return screenPoint;
        return Vector2.zero;
    }
}

public class ThirdPersonView : MonoBehaviour
{
    public Player localPlayer;

    // Static Config
    private LayerMask _eyeCameraHitMask;
    private readonly Vector3 _eyeCameraOffset = new(0f, 0.1f, 0f);

    // State
    private bool _thirdPersonEnabled;
    private CameraPositionEnum _cameraPosition;
    private int _cameraStance;
    private Vector3 _currentOffset;

    private bool _aimFlag;
    private bool _sprintFlag;

    private AdsModeEnum _adsModeOptic;
    private AdsModeEnum _adsModeBasic;

    private PositionSolver _positionSolver;

    private Player.ItemHandsController _handsController;
    private Player.FirearmController _firearmController;

    private SharedGameSettingsClass _gameSettings;

    public void Awake()
    {
        _thirdPersonEnabled = Plugin.PointOfViewDefault.Value == PointOfViewEnum.ThirdPerson;
        _cameraPosition = Plugin.CameraPositionDefault.Value;
        _cameraStance = (int)Plugin.CameraStanceDefault.Value;

        _adsModeOptic = Plugin.AdsModeOptic.Value;
        _adsModeBasic = Plugin.AdsModeBasic.Value;

        Plugin.AdsModeOptic.SettingChanged += (_, _) => _adsModeOptic = Plugin.AdsModeOptic.Value;
        Plugin.AdsModeBasic.SettingChanged += (_, _) => _adsModeBasic = Plugin.AdsModeBasic.Value;

        // Hit mask for the camera root. Remove players from this to avoid colliding with ourselves, duh
        _eyeCameraHitMask = GClass3449.HitMask.value & ~(1 << LayerMask.NameToLayer("HitCollider"));

        _gameSettings = Singleton<SharedGameSettingsClass>.Instance;

        _positionSolver = new PositionSolver(
            new AdvectionSolver(
                new CircleScan(12, 0.7f, 2f),
                new LineScan(1.0f, 0.05f)
            ),
            new AdvectionSolver(
                new CircleScan(10, 0.25f, 2f, centerRadius: 0.09f),
                new LineScan(0.5f, 0.025f)
            ),
            new AdvectionSolver(
                new CircleScan(8, 0.125f, 2f, centerRadius: 0.05f),
                new LineScan(0.3f, 0.015f)
            ), 0.2f, 0.05f
        );
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(Plugin.ThirdPersonToggleKey.Value))
            _thirdPersonEnabled = !_thirdPersonEnabled;

        if (Input.GetKeyDown(Plugin.CameraShoulderKey.Value))
            _cameraPosition = _cameraPosition != CameraPositionEnum.Shoulder ? CameraPositionEnum.Shoulder : Plugin.CameraPositionDefault.Value;

        if (Input.GetKeyDown(Plugin.CameraStanceToggleKey.Value))
            _cameraStance *= -1;

        if (Input.GetKeyDown(Plugin.CameraStanceLeftKey.Value))
            _cameraStance = -1;

        if (Input.GetKeyDown(Plugin.CameraStanceRightKey.Value))
            _cameraStance = 1;
    }

    public void UpdateCamera()
    {
        // NB: Has to be LateUpdate as doing all this in Update can cause the camera to occasionally clip into the wall when mousing violently.
        HandleInputs();

        if (localPlayer.CameraPosition == null || !localPlayer.HealthController.IsAlive)
            return;

        _handsController = localPlayer.HandsController as Player.ItemHandsController;
        _firearmController = localPlayer.HandsController as Player.FirearmController;

        var isAiming = _handsController != null && _handsController.IsAiming;

        if (Plugin.CameraStanceSwapOnLeanEnabled.Value)
        {
            _cameraStance = localPlayer.MovementContext._tilt switch
            {
                < 0 => -1,
                > 0 => 1,
                _ => _cameraStance
            };
        }

        if (Plugin.GunStanceSync.Value == GunStanceSyncEnum.Cam)
        {
            if (_firearmController != null)
            {
                if ((localPlayer.MovementContext.LeftStanceEnabled && _cameraStance > 0f)
                    || (!localPlayer.MovementContext.LeftStanceEnabled && _cameraStance < 0f))
                    _firearmController.ChangeLeftStance();
            }
        }

        var currentScopeIsOptic = _firearmController != null && localPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic;

        if (Input.GetKeyDown(Plugin.AdsModeSwapKey.Value))
        {
            if (currentScopeIsOptic)
                _adsModeOptic = _adsModeOptic == AdsModeEnum.FirstPerson ? AdsModeEnum.Shoulder : AdsModeEnum.FirstPerson;
            else
                _adsModeBasic = _adsModeBasic == AdsModeEnum.FirstPerson ? AdsModeEnum.Shoulder : AdsModeEnum.FirstPerson;
        }

        // Force to first person when mounting stationary weapons. They tend to glitch out otherwise.
        if (!_thirdPersonEnabled || localPlayer.MovementContext.StationaryWeapon != null)
        {
            if (localPlayer.PointOfView != EPointOfView.ThirdPerson) return;

            UpdatePointOfView(EPointOfView.FirstPerson);
            return;
        }

        var adsModeSelected = currentScopeIsOptic
            ? _adsModeOptic
            : _adsModeBasic;

        switch (adsModeSelected)
        {
            case AdsModeEnum.FirstPerson:
            {
                switch (isAiming)
                {
                    case true when localPlayer.PointOfView == EPointOfView.FirstPerson:
                        return;
                    case true when localPlayer.PointOfView == EPointOfView.ThirdPerson:
                        UpdatePointOfView(EPointOfView.FirstPerson);
                        return;
                    case false when _aimFlag:
                        _cameraPosition = Plugin.CameraPositionDefault.Value;
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 1f);
                        ResetFoV();
                        break;
                }

                break;
            }
            case AdsModeEnum.Shoulder:
            {
                switch (isAiming)
                {
                    case true when !_aimFlag:
                        _cameraPosition = CameraPositionEnum.Shoulder;
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 0);
                        ScopeFoV();
                        break;
                    case false when _aimFlag:
                        _cameraPosition = Plugin.CameraPositionDefault.Value;
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 1f);
                        ResetFoV();
                        break;
                }

                break;
            }
            case AdsModeEnum.None:
                switch (isAiming)
                {
                    case true when !_aimFlag:
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 0);
                        ScopeFoV();
                        break;
                    case false when _aimFlag:
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 1f);
                        ResetFoV();
                        break;
                }

                break;
            default:
                Plugin.Log.LogError($"Unknown ADS mode selected: {adsModeSelected}");
                break;
        }

        _aimFlag = isAiming;

        HandleThirdPerson();
    }

    private void HandleThirdPerson()
    {
        if (localPlayer.PointOfView != EPointOfView.ThirdPerson)
        {
            UpdatePointOfView(EPointOfView.ThirdPerson);
        }

        // The offset vector is passed by value, which means it's safe to modify it here
        var desiredCameraOffset = _cameraPosition switch
        {
            CameraPositionEnum.Main => Plugin.CameraMainOffset.Value,
            CameraPositionEnum.Shoulder => Plugin.CameraShoulderOffset.Value,
            _ => Plugin.CameraMainOffset.Value
        };
        desiredCameraOffset.x *= _cameraStance;

        if (localPlayer.IsSprintEnabled)
        {
            if (!_sprintFlag)
                AdjustFoV(Plugin.SprintFovChange.Value, Plugin.SprintFovChangeTime.Value);
        }
        else if (_sprintFlag)
        {
            ResetFoV();
        }

        _sprintFlag = localPlayer.IsSprintEnabled;

        var eyeTransform = localPlayer.CameraPosition.parent;
        var eyeCameraPos = eyeTransform.TransformPoint(_eyeCameraOffset);

        _currentOffset = _positionSolver.Solve(
            eyeTransform, _currentOffset, desiredCameraOffset, eyeCameraPos, _eyeCameraHitMask
        );

        localPlayer.CameraPosition.localPosition = _currentOffset;
    }

    private void UpdatePointOfView(EPointOfView value)
    {
        localPlayer.PointOfView = value;

        if (value == EPointOfView.ThirdPerson)
        {
            // We force weapon handling to be first person. This allows optic sight rendering to work correctly (they don't render properly otherwise).
            localPlayer.ProceduralWeaponAnimation.PointOfView = EPointOfView.FirstPerson;
            // Turnaway must be forced back to 3rd person otherwise the hands misbehave
            localPlayer.ProceduralWeaponAnimation.TurnAway.PointOfView = value;

            localPlayer.POM.CameraCollider.enabled = false;

            // Force our own custom weapon animation strategy that enables proper recoil
            localPlayer.ProceduralWeaponAnimation.SetStrategy(StaticData.CustomAnimStrategy);
            CameraClass.Instance.Camera.nearClipPlane = 0.005f;
            
            // Re-enable recoil and hit reactions in third person
            if (localPlayer.HitReaction != null)
            {
                localPlayer.HitReaction.enabled = true;
            }
        }
        else
        {
            localPlayer.POM.CameraCollider.enabled = true;
            // Reset the offset back to the BSG default
            localPlayer.CameraPosition.localPosition = _currentOffset = localPlayer.ProceduralWeaponAnimation.HandsContainer.CameraOffset;

            // Explicitly set the pwa strategy to mounted in this case, because the game would just set it to the FP strategy and that's incorrect
            if (value == EPointOfView.FirstPerson)
            {
                localPlayer.ProceduralWeaponAnimation.SetStrategy(
                    localPlayer.ProceduralWeaponAnimation.IsMountedState
                        ? localPlayer.MovementContext._mountingStrategy
                        : StaticData.FpAnimStrategy
                );
            }

            CameraClass.Instance.Camera.nearClipPlane = 0.03f;
        }

        if (_firearmController == null)
            _firearmController.UpdateSensitivity();
    }

    private void ScopeFoV()
    {
        var currentScopeIsOptic = localPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic;
        var fov = currentScopeIsOptic ? 35 : _gameSettings.Game.Settings.FieldOfView.Value + Plugin.AdsBasicFovChange.Value;
        SetFoV(fov);
    }

    private void AdjustFoV(int adjustment, float time)
    {
        CameraClass.Instance.SetFov(_gameSettings.Game.Settings.FieldOfView.Value + adjustment, time);
    }

    private static void SetFoV(int fov)
    {
        CameraClass.Instance.SetFov(fov, Plugin.AdsFovChangeTime.Value);
    }

    private void ResetFoV()
    {
        CameraClass.Instance.SetFov(_gameSettings.Game.Settings.FieldOfView.Value, Plugin.AdsFovChangeTime.Value);
    }

    public void OnGUI()
    {
        if (!localPlayer.HealthController.IsAlive)
            return;

        // CollisionDebug.DrawCollisionInfo(_positionSolver.Phase1.CircleScan);

        if (Plugin.DebugUIEnabled.Value)
        {
            var pwa = localPlayer.ProceduralWeaponAnimation;
            var pwaStrat = Traverse.Create(pwa).Field("_strategy").GetValue();
            var leftStanceCurve = Traverse.Create(pwa).Field("_leftStanceCurrentCurveValue").GetValue();

            var hitReactionsEnabled = false;
            
            if (localPlayer.HitReaction != null)
                hitReactionsEnabled = localPlayer.HitReaction.enabled;
            
            // ReSharper disable 
            var rect = DebugUI.Label(new Vector2(50, 50),
                $"PL POV: {localPlayer.PointOfView} TP Enabled: {_thirdPersonEnabled} PWA POV: {pwa.PointOfView} PWA Strat: {pwaStrat}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height),
                $"IsAiming: {pwa.IsAiming} MoveWeapCloser{pwa._shouldMoveWeaponCloser} IsMounted: {pwa.IsMountedState}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"ADS Mode Optic: {_adsModeOptic} Basic: {_adsModeBasic}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SmoothTilt: {pwa.SmoothedTilt} PossibleTilt{pwa.PossibleTilt}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"LeftStanceCurve: {leftStanceCurve}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"StationaryWpn{localPlayer.MovementContext.StationaryWeapon}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height),
                $"InteractionRay Pos: {localPlayer.InteractionRay.origin} Dir: {localPlayer.InteractionRay.direction}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Hit Reactions: {hitReactionsEnabled}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"HandCtr: {_handsController}", centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"FACtr: {_firearmController}", centered: false);
            var cameraController = localPlayer.gameObject.GetComponent<PlayerCameraController>();
            if (cameraController != null)
            {
                var pcaStrategy = Traverse.Create(cameraController).Field("gclass3404_0").GetValue<GClass3404>();
                rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"PCA Strat: {pcaStrategy}", centered: false);
            }

            if (_firearmController != null)
            {
                rect = DebugUI.Label(new Vector2(50, rect.y + rect.height),
                    $"Aim Sens: {_firearmController.AimingSensitivity} Smooth Sens: {_firearmController.AimingSmoothSensitivity}", centered: false);
                rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Current Scope: {pwa.CurrentScope}", centered: false);
            }

            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Cam Near Clip Plane: {CameraClass.Instance.Camera.nearClipPlane}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height),
                $"Line Scan Radius P1: {_positionSolver.Phase1.LineScanRadius} P2: {_positionSolver.Phase2.LineScanRadius} P3: {_positionSolver.Phase3.LineScanRadius}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Cam Offset: {_currentOffset} PWA Offset: {pwa.HandsContainer.CameraOffset}",
                centered: false);
            rect = DebugUI.Label(new Vector2(50, rect.y + rect.height),
                $"Cam Pos: {localPlayer.CameraPosition.position} Local: {localPlayer.CameraPosition.localPosition}", centered: false);
            DebugUI.Label(new Vector2(50, rect.y + rect.height),
                $"Cam Rot: {localPlayer.CameraPosition.rotation} Local: {localPlayer.CameraPosition.localRotation}", centered: false);
        }

        if (!Plugin.CrosshairEnabled.Value || localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        if (_firearmController == null)
            return;

        if (!_firearmController.IsAiming && Plugin.CrosshairAdsOnlyEnabled.Value)
            return;

        // Only solid/opaque stuff
        // const int layerMaskVisCheck = 0b0000_00100_0001_0001_1000_0000_0000;
        const int layerMaskVisCheck = 0b0000_00000_0001_0001_1000_0000_0000;
        var ray = new Ray(_firearmController.CurrentFireport.position, _firearmController.WeaponDirection);

        Vector3 targetPoint;

        if (Physics.Raycast(ray, out var hitInfo, 10000f, layerMaskVisCheck))
        {
            targetPoint = hitInfo.point;
        }
        else
        {
            targetPoint = _firearmController.CurrentFireport.position + 10000f * _firearmController.WeaponDirection;
        }

        var screenPosition = CameraClass.Instance.Camera.WorldPointToVisibleScreenPoint(targetPoint);

        if (screenPosition == Vector2.zero)
            return;

        DrawCrosshair(screenPosition, 5, Plugin.CrosshairColor.Value, Plugin.CrosshairThickness.Value);
    }

    private static void DrawCrosshair(Vector2 position, float size, Color color, float thickness)
    {
        var colorBackup = GUI.color;
        GUI.color = color;

        var texture = Texture2D.whiteTexture;
        GUI.DrawTexture(new Rect(position.x - size, position.y, size * 2 + thickness, thickness), texture);
        GUI.DrawTexture(new Rect(position.x, position.y - size, thickness, size * 2 + thickness), texture);

        GUI.color = colorBackup;
    }
}