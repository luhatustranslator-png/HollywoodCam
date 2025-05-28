using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.UI;
using HarmonyLib;
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
    
    private Player.ItemHandsController _handsController;
    private Player.FirearmController _firearmController;

    private bool _thirdPersonEnabled;
    private CameraPositionEnum _cameraPosition;
    private int _cameraStance;

    private LayerMask _hitMaskRoot;
    private LayerMask _hitMaskTarget;

    private bool _aimFlag;
    private bool _sprintFlag;
    private EPointOfView _currentPointOfView;
    private Vector3 _cameraVelocity = Vector3.zero;

    private SharedGameSettingsClass _gameSettings;
    private static readonly CustomAnimStrategy CustomAnimStrategy = new();

    public void Awake()
    {
        _currentPointOfView = EPointOfView.FirstPerson;
        _thirdPersonEnabled = Plugin.PointOfViewDefault.Value == PointOfViewEnum.ThirdPerson;
        _cameraPosition = Plugin.CameraPositionDefault.Value;
        _cameraStance = (int)Plugin.CameraStanceDefault.Value;

        // Hit mask for the aim target
        _hitMaskTarget = GClass3449.HitMask.value;
        // Hit mask for the camera root. Remove players from this to avoid colliding with ourselves, duh
        _hitMaskRoot = GClass3449.HitMask.value & ~(1 << LayerMask.NameToLayer("HitCollider"));

        _gameSettings = Singleton<SharedGameSettingsClass>.Instance;
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
            if (_currentPointOfView != EPointOfView.ThirdPerson) return;

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
                    case true when _currentPointOfView == EPointOfView.FirstPerson:
                        return;
                    case true when _currentPointOfView == EPointOfView.ThirdPerson:
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
        if (_currentPointOfView != EPointOfView.ThirdPerson)
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

        var cameraSpeed = Plugin.CameraSwitchSpeed.Value;

        if (localPlayer.IsSprintEnabled)
        {
            if (!_sprintFlag)
                AdjustFoV(Plugin.SprintFovChange.Value, Plugin.SprintFovChangeTime.Value);

            desiredCameraOffset.Scale(Plugin.SprintOffsetFactor.Value);
            cameraSpeed = Plugin.SprintCameraSwitchSpeed.Value;
        }
        else if (_sprintFlag)
        {
            ResetFoV();
        }

        _sprintFlag = localPlayer.IsSprintEnabled;

        var collisionDetected = TryHandleCollisions(desiredCameraOffset, out var actualCameraOffset);

        localPlayer.CameraPosition.localPosition = Vector3.SmoothDamp(
            localPlayer.CameraPosition.localPosition, actualCameraOffset, ref _cameraVelocity, Time.deltaTime, Plugin.CameraSwitchSpeed.Value
        );

        // This is a bit motion sickness inducing
        // if (_sprintFlag)
        //     return;
        //
        // var fc = localPlayer.HandsController as Player.FirearmController;
        //
        // if (fc == null)
        //     return;
        //
        // var ray = new Ray(fc.CurrentFireport.position, fc.WeaponDirection);
        // if (!Physics.Raycast(ray, out var hitInfo, 100000, GClass3449.HitMask))
        //     return;
        //
        // var aimVector = hitInfo.point - localPlayer.CameraPosition.position;
        //
        // localPlayer.CameraPosition.rotation = Quaternion.Slerp(localPlayer.CameraPosition.rotation, Quaternion.LookRotation(aimVector), Time.deltaTime * 5);

        // if (collisionDetected)
        // {
        //     localPlayer.CameraPosition.localPosition = actualCameraOffset;
        // }
        // else
        // {
        //                 
        // }
    }

    private bool TryHandleCollisions(Vector3 desiredCameraOffset, out Vector3 actualCameraOffset)
    {
        var parentTransform = localPlayer.CameraPosition.parent;
        // Transform the desired offset to world coordinates
        var desiredWorldPos = parentTransform.TransformPoint(desiredCameraOffset);
        // Get the vector between the camera parent and the desired position
        var offsetVector = desiredWorldPos - parentTransform.position;
        // Ray cast parameters
        var ray = new Ray(parentTransform.position, offsetVector.normalized);

        // Try to do a sphere cast first, if that hits nothing, do another raycast as the spherecast might've clipped behind a wall.
        if (!Physics.SphereCast(ray, 0.25f, out var hitInfo, offsetVector.magnitude, _hitMaskRoot))
        {
            if (!Physics.Raycast(ray, out hitInfo, offsetVector.magnitude, _hitMaskRoot))
            {
                // We hit nothing, continue with the desired offset as the actual offset
                actualCameraOffset = desiredCameraOffset;
                return false;
            }
        }

        // Find the closest point to the collision on the line between the parent and the desired position 
        var adjustedWorldPos = Geometry.ClosestPointOnLine(parentTransform.position, desiredWorldPos, hitInfo.point);
        // Translate back to local/relative offset
        actualCameraOffset = parentTransform.InverseTransformPoint(adjustedWorldPos);
        return true;
    }

    private void UpdatePointOfView(EPointOfView value)
    {
        UpdatePlayerPointOfView(value);

        if (value == EPointOfView.ThirdPerson)
        {
            ConsoleScreen.Log($"TP Enabled");
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
            ConsoleScreen.Log($"TP Disabled");
            localPlayer.POM.CameraCollider.enabled = true;
            localPlayer.CameraPosition.localPosition = Vector3.zero;
        }
    }

    private void UpdatePlayerPointOfView(EPointOfView value)
    {
        _currentPointOfView = value;
        localPlayer.PointOfView = value;

        // if (playerBody.PointOfView.Value == value)
        // return;
        // playerBody.PointOfView.Value = value;
        // localPlayer.CalculateScaleValueByFov((int) Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView);
        // localPlayer.SetCompensationScale();
        // if (value == EPointOfView.ThirdPerson)
        // localPlayer.PlayerBones.Ribcage.Original.localScale = new Vector3(1f, 1f, 1f);
        // localPlayer.MovementContext.PlayerAnimatorPointOfView(value);
        // localPlayer.PointOfViewChanged?.Invoke();
        // playerBody.UpdatePlayerRenders(value, localPlayer.Side);

        // Update the internal field directly. This bypasses the event handlers that'd mess up camera positioning, etc.
        // if (value == EPointOfView.ThirdPerson)
        // {
        //     localPlayer.PointOfView = value;
        //
        //     if (_playerBody == null)
        //     {
        //         _playerBody = Traverse.Create(localPlayer).Field("_playerBody").GetValue<PlayerBody>();
        //     }
        //
        //     _playerBody.PointOfView.gparam_0 = EPointOfView.FirstPerson;
        // }
        // else
        // {
        //     localPlayer.PointOfView = EPointOfView.ThirdPerson;
        //     localPlayer.PointOfView = EPointOfView.FirstPerson;
        // }
        // playerBody.PlayerSide.Value = localPlayer.Side;

        // localPlayer.ProceduralWeaponAnimation.PointOfView = value;
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
        if (!Plugin.CrosshairEnabled.Value || _currentPointOfView == EPointOfView.FirstPerson)
            return;

        if (_firearmController == null)
            return;

        if (!_firearmController.IsAiming && Plugin.CrosshairAdsOnlyEnabled.Value)
            return;

        var ray = new Ray(_firearmController.CurrentFireport.position, _firearmController.WeaponDirection);
        if (!Physics.Raycast(ray, out var hitInfo, 100000, GClass3449.HitMask))
            return;

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