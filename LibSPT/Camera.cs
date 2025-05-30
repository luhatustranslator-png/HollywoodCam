using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.UI;
using HarmonyLib;
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
    private LayerMask _hitMaskRoot;
    private LayerMask _hitMaskTarget;
    private readonly Vector3 _rootCameraOffset = new(0f, 0.15f, 0f);

    // State
    private bool _thirdPersonEnabled;
    private CameraPositionEnum _cameraPosition;
    private int _cameraStance;

    private bool _aimFlag;
    private bool _sprintFlag;

    private Vector3 _cameraVelocity = Vector3.zero;
    private Vector3 _targetPosition = Vector3.zero;
    private bool _targetValid = false;
    private CollisionField _collisionField;

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
        _hitMaskTarget = GClass3449.HitMask.value;
        // Hit mask for the camera root. Remove players from this to avoid colliding with ourselves, duh
        _hitMaskRoot = GClass3449.HitMask.value & ~(1 << LayerMask.NameToLayer("HitCollider"));

        _gameSettings = Singleton<SharedGameSettingsClass>.Instance;

        _collisionField = new CollisionField(8, 0.25f, 0.01f, 0.05f);
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

        if (_firearmController != null)
        {
            var ray = new Ray(_firearmController.CurrentFireport.position, _firearmController.WeaponDirection);
            if (Physics.Raycast(ray, out var hitInfo, 100000, GClass3449.HitMask))
            {
                _targetPosition = hitInfo.point;
                _targetValid = true;
            }
            else
            {
                _targetValid = false;
            }
        }
        else
        {
            _targetValid = false;
        }

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

        // This is a bit motion sickness inducing?
        // TODO: Add a sprint decay factor here that phases out the rotation suppression over 1-2 seconds
        if (_sprintFlag)
            return;

        var fc = localPlayer.HandsController as Player.FirearmController;

        if (fc == null)
            return;

        var ray = new Ray(fc.CurrentFireport.position, fc.WeaponDirection);
        if (!Physics.Raycast(ray, out var hitInfo, 100000, GClass3449.HitMask))
            return;

        var aimVector = hitInfo.point - localPlayer.CameraPosition.position;

        localPlayer.CameraPosition.rotation =
            Quaternion.Slerp(localPlayer.CameraPosition.rotation, Quaternion.LookRotation(aimVector), Time.deltaTime);
    }

    private void HandleCollision(Vector3 desiredOffset)
    {
        var parentTransform = localPlayer.CameraPosition.parent;
        var rootPos = parentTransform.TransformPoint(_rootCameraOffset);
        var desiredPos = parentTransform.TransformPoint(desiredOffset);

        // var actualPos = desiredPos;
        // Phase1CollisionSolver(rootPos, ref actualPos, 0.2f, 8, 0.5f);

        // var actualPos = SphereCastBump(desiredPos, rootPos, _hitMaskRoot, 0.2f, 1f);
        var actualPos = desiredPos;

        var actualOffset = parentTransform.InverseTransformPoint(actualPos);

        localPlayer.CameraPosition.localPosition = Vector3.SmoothDamp(
            localPlayer.CameraPosition.localPosition, actualOffset, ref _cameraVelocity, Time.deltaTime, Plugin.CameraSpeed.Value
        );

        if (!_targetValid)
            return;

        var rootVector = rootPos - localPlayer.CameraPosition.position;
        var targetAdj = localPlayer.CameraPosition.position +
                        rootVector.magnitude * (_targetPosition - localPlayer.CameraPosition.position).normalized;

        _collisionField.Update(localPlayer.CameraPosition, rootPos, targetAdj, _hitMaskRoot, _hitMaskTarget);
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
        localPlayer.PointOfView = value;
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
        CollisionDebug.DrawCollisionFieldInfo(_collisionField);
        
        if (!Plugin.CrosshairEnabled.Value || localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        if (_firearmController == null || !_targetValid)
            return;

        if (!_firearmController.IsAiming && Plugin.CrosshairAdsOnlyEnabled.Value)
            return;

        var screenPosition = CameraClass.Instance.Camera.WorldPointToVisibleScreenPoint(_targetPosition);

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

    /*
     * Attic
     */
    private bool Phase1CollisionSolver(Vector3 rootPos, ref Vector3 desiredPos, float radius, int attempts, float alpha)
    {
        var bumpFactor = 1f;

        for (var i = 0; i < attempts; i++)
        {
            if (Phase1CollisionStep(rootPos, ref desiredPos, radius, bumpFactor))
                return true;

            bumpFactor *= alpha;
        }

        return false;
    }

    private bool Phase1CollisionStep(Vector3 rootPos, ref Vector3 desiredPos, float radius, float bumpFactor)
    {
        desiredPos = SphereCastBump(desiredPos, rootPos, _hitMaskRoot, radius, bumpFactor);

        Vector3 targetPosAdj;

        if (_targetValid)
        {
            // Offset the target position to avoid noisy collision events as the target position is by definition a collision point
            targetPosAdj = _targetPosition + 10f * radius * (desiredPos - _targetPosition).normalized;
            desiredPos = SphereCastBump(desiredPos, targetPosAdj, _hitMaskTarget, radius, bumpFactor);
        }

        var rootVisible = SphereCastVisCheck(desiredPos, rootPos, radius, _hitMaskRoot);

        if (!_targetValid) return rootVisible;

        // Offset the target position to avoid noisy collision events as the target position is by definition a collision point
        targetPosAdj = _targetPosition + 10f * radius * (desiredPos - _targetPosition).normalized;
        var targetVisible = SphereCastVisCheck(desiredPos, targetPosAdj, radius, _hitMaskTarget);

        return rootVisible && targetVisible;
    }

    private static Vector3 SphereCastBump(Vector3 cameraPos, Vector3 targetPos, LayerMask layerMask, float radius, float bumpFactor)
    {
        var backCast = cameraPos - targetPos;

        if (!Physics.SphereCast(targetPos, radius, backCast.normalized, out var hitInfo, backCast.magnitude, layerMask)) return cameraPos;

        var tangentPoint = Geometry.ClosestPointOnLine(targetPos, cameraPos, hitInfo.point);
        // var bump = tangentPoint - hitInfo.point;
        // var bumpAmount = radius - bump.magnitude;
        // return cameraPos + bumpFactor * bumpAmount * bump.normalized;
        return tangentPoint;
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

    private static bool SphereCastVisCheck(Vector3 origin, Vector3 target, float radius, LayerMask layerMask)
    {
        var aimVector = target - origin;
        return Physics.SphereCast(origin, radius, aimVector.normalized, out _, aimVector.magnitude, layerMask);
    }
}