using Comfort.Common;
using EFT;
using HollywoodCam.Collision;
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
    private LayerMask _targetHitMask;
    private readonly Vector3 _eyeCameraOffset = new(0f, 0.1f, 0f);
    private readonly Vector3 _aimOriginOffset = new(0.2f, 0f, 0f);

    private float _aimOriginZBump;
    private float _aimOriginZBumpVelocity;
    private const float AimOriginZBumpMax = 0.25f;

    // State
    private bool _thirdPersonEnabled;
    private CameraPositionEnum _cameraPosition;
    private int _cameraStance;

    private bool _aimFlag;
    private bool _sprintFlag;

    private Vector3 _aimTarget = Vector3.zero;

    private PositionSolver _positionSolver;

    private Player.ItemHandsController _handsController;
    private Player.FirearmController _firearmController;

    private SharedGameSettingsClass _gameSettings;
    private static readonly CustomAnimStrategy CustomAnimStrategy = new();

    public void Awake()
    {
        _thirdPersonEnabled = Plugin.PointOfViewDefault.Value == PointOfViewEnum.ThirdPerson;
        _cameraPosition = Plugin.CameraPositionDefault.Value;
        _cameraStance = (int)Plugin.CameraStanceDefault.Value;

        // Hit mask for the aim target
        _targetHitMask = GClass3449.HitMask.value;
        // Hit mask for the camera root. Remove players from this to avoid colliding with ourselves, duh
        _eyeCameraHitMask = GClass3449.HitMask.value & ~(1 << LayerMask.NameToLayer("HitCollider"));

        _gameSettings = Singleton<SharedGameSettingsClass>.Instance;

        _positionSolver = new PositionSolver(
            new AdvectionSolver(
                new GradientScanSwap(10, 0.25f),
                new LineScan(1f, 20)
            ),
            new AdvectionSolver(
                new GradientScanSwap(8, 0.15f),
                new LineScan(1f, 20)
            )
        );
    }

    private void HandleInputs()
    {
        if (Input.GetKeyDown(Plugin.ThirdPersonToggleKey.Value))
            _thirdPersonEnabled = !_thirdPersonEnabled;

        if (Input.GetKeyDown(Plugin.CameraShoulderKey.Value))
            _cameraPosition = _cameraPosition != CameraPositionEnum.Shoulder ? CameraPositionEnum.Shoulder : Plugin.CameraPositionDefault.Value;

        if (Input.GetKeyDown(Plugin.CameraStanceLeftKey.Value))
            _cameraStance = -1;

        if (Input.GetKeyDown(Plugin.CameraStanceRightKey.Value))
            _cameraStance = 1;
    }

    public void Update()
    {
        HandleInputs();

        if (localPlayer.CameraPosition == null)
            return;

        _handsController = localPlayer.HandsController as Player.ItemHandsController;
        _firearmController = localPlayer.HandsController as Player.FirearmController;

        var isAiming = _handsController != null && _handsController.IsAiming;

        if (!_thirdPersonEnabled)
        {
            if (localPlayer.PointOfView != EPointOfView.ThirdPerson) return;

            UpdatePointOfView(EPointOfView.FirstPerson);
            return;
        }

        var adsModeSelected = localPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic ? Plugin.AdsModeOptic.Value : Plugin.AdsModeBasic.Value;

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
                    case false when _aimFlag && _cameraPosition == CameraPositionEnum.Shoulder:
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

        HandleCollision(desiredCameraOffset);
    }

    private void HandleCollision(Vector3 desiredOffset)
    {
        var aimOriginOffset = _aimOriginOffset;

        // Switch immediately when the flags are on, but add a SmoothDamp once off to avoid sporadic self collision.
        if (_aimFlag || _sprintFlag)
        {
            _aimOriginZBump = AimOriginZBumpMax;
            _aimOriginZBumpVelocity = 0f;
        }
        else
        {
            _aimOriginZBump = Mathf.SmoothDamp(_aimOriginZBump, 0f, ref _aimOriginZBumpVelocity, 0.5f);
        }

        aimOriginOffset.z += _aimOriginZBump;

        var eyeTransform = localPlayer.CameraPosition.parent;
        var aimOriginPos = eyeTransform.TransformPoint(aimOriginOffset);
        var eyeCameraPos = eyeTransform.TransformPoint(_eyeCameraOffset);

        var aimTargetHitMask = _sprintFlag ? _eyeCameraHitMask : _targetHitMask;

        var ray = new Ray(aimOriginPos, eyeTransform.forward);
        if (Physics.Raycast(ray, out var hitInfo, 5f, aimTargetHitMask))
        {
            // _aimTarget = Geometry.ClosestPointOnLine(aimOriginPos, aimOriginPos + eyeTransform.forward * 24.5f, hitInfo.point);

            // Offset the target back by half a meter so that we don't re-hit the same hit point later.
            _aimTarget = hitInfo.point - 0.5f * eyeTransform.forward;
        }
        else
        {
            _aimTarget = aimOriginPos + eyeTransform.forward * 4.5f;
        }

        var actualDesiredOffset = _positionSolver.Solve(
            eyeTransform, localPlayer.CameraPosition.localPosition, desiredOffset, eyeCameraPos, _aimTarget, _eyeCameraHitMask, aimTargetHitMask
        );

        localPlayer.CameraPosition.localPosition = actualDesiredOffset;

        // TODO: Tune this
        var aimVector = _aimTarget - localPlayer.CameraPosition.position;
        var rotationSpeed = 10 * Mathf.InverseLerp(100f, 900f, aimVector.sqrMagnitude);
        localPlayer.CameraPosition.rotation = Quaternion.Slerp(
            localPlayer.CameraPosition.rotation, Quaternion.LookRotation(aimVector), rotationSpeed * Time.deltaTime
        );
    }

    private void UpdatePointOfView(EPointOfView value)
    {
        UpdatePlayerPointOfView(value);

        if (value == EPointOfView.ThirdPerson)
        {
            localPlayer.POM.CameraCollider.enabled = false;

            // Re-enable recoil and hit reactions in third person
            if (localPlayer.HitReaction != null)
            {
                localPlayer.HitReaction.enabled = true;
            }

            // Force our own custom weapon animation strategy that enables proper recoil
            localPlayer.ProceduralWeaponAnimation.SetStrategy(CustomAnimStrategy);
        }
        else
        {
            localPlayer.POM.CameraCollider.enabled = true;
            localPlayer.CameraPosition.localPosition = Vector3.zero;
        }
    }

    private void UpdatePlayerPointOfView(EPointOfView value)
    {
        localPlayer.PointOfView = value;
        localPlayer.PointOfView = value;
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
        // CollisionDebug.DrawCollisionFieldInfo(_collisionField);

        if (!Plugin.CrosshairEnabled.Value || localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        var aimScreenPosition = CameraClass.Instance.Camera.WorldPointToVisibleScreenPoint(_aimTarget);
        DrawCrosshair(aimScreenPosition, 5, Color.red, 2);

        if (_firearmController == null)
            return;

        if (!_firearmController.IsAiming && Plugin.CrosshairAdsOnlyEnabled.Value)
            return;

        var ray = new Ray(_firearmController.CurrentFireport.position, _firearmController.WeaponDirection);
        if (!Physics.Raycast(ray, out var hitInfo, 100000, GClass3449.HitMask)) return;

        var screenPosition = CameraClass.Instance.Camera.WorldPointToVisibleScreenPoint(hitInfo.point);

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