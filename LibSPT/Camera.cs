using System;
using Comfort.Common;
using EFT;
using EFT.Animations;
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

/// <summary>
/// This class is a carbon copy of GClass889, with the exception of ProcessEffectors and ApplyTransformations that come from GClass888.
/// The 889 is used for third person view and 888 is first person. By default, the third person strategy doesn't apply proper recoil forces.
/// </summary>
public class CustomAnimStrategy : GInterface38
{
    public void ApplyCameraTransformations(ProceduralWeaponAnimation pwa, float dt)
    {
    }

    public void ProcessEffectors(ProceduralWeaponAnimation pwa, float deltaTime, int nFixedFrames = 1)
    {
        if (nFixedFrames < 0 || pwa.HandsContainer.WeaponRootAnim == null || !pwa.enabled || Mathf.Approximately(deltaTime, 0.0f))
            return;

        deltaTime /= nFixedFrames;

        for (var index = 0; index < nFixedFrames; ++index)
        {
            if ((pwa.Mask & EProceduralAnimationMask.MotionReaction) != 0)
            {
                pwa.MotionReact.FixedTracking(deltaTime);
                pwa.MotionReact.Process(deltaTime);
            }

            if ((pwa.Mask & EProceduralAnimationMask.ForceReaction) != 0)
                pwa.ForceReact.Process(deltaTime);
            if ((pwa.Mask & EProceduralAnimationMask.Breathing) != 0)
                pwa.Breath.Process(deltaTime);
            if ((pwa.Mask & EProceduralAnimationMask.Walking) != 0)
                pwa.Walk.Process(deltaTime);
            if ((pwa.Mask & EProceduralAnimationMask.HandShake) != 0)
                pwa.HandShakeEffector.Process(deltaTime);
            if ((pwa.Mask & EProceduralAnimationMask.DrawDown) == 0 || pwa.ActiveBlends.Count > 0)
                pwa.TurnAway.OverlapDepth = 0.0f;
            pwa.TurnAway.LeftStance = pwa.LeftStance;
            pwa.TurnAway.InMountState = pwa.IsMountedState;
            pwa.TurnAway.Process(deltaTime);
            pwa.HandsContainer.HandsPosition.FixedUpdate(deltaTime);
            pwa.HandsContainer.HandsRotation.FixedUpdate(deltaTime);
            pwa.HandsContainer.SwaySpring.Process(deltaTime);
            pwa.Shootingg.CurrentRecoilEffect.FixedUpdate(deltaTime);
        }
    }

    public void ApplyTransformations(ProceduralWeaponAnimation pwa, float dt)
    {
        pwa.ZeroAdjustments();
        pwa.UpdateAimWeight(dt);
        pwa.BlendAnimatorPose(dt);
        pwa.ApplyPosition();
        // NB: This line enables the proper recoil response. In the regular third person strategy, BSG uses ApplySimpleRotation
        pwa.ApplyComplexRotation(dt);
        pwa.ApplyTacticalReloadTransformations();
        pwa.AvoidObstacles();
    }

    public void LateTransformations(ProceduralWeaponAnimation pwa, float dt)
    {
    }

    public void ApplyFovAdjustments(ProceduralWeaponAnimation proceduralWeaponAnimation, Player player)
    {
        player.RibcageScaleCurrent = 1f;
    }

    public void ResetFovAdjustments(ProceduralWeaponAnimation proceduralWeaponAnimation, Player player)
    {
        if (Mathf.Approximately(player.PlayerBones.Ribcage.Original.localScale.z, 1f))
            return;
        player.PlayerBones.Ribcage.Original.localScale = Vector3.one;
        player.HandsController.HandsHierarchy.Self.localScale = Vector3.one;
    }

    public void OpticCalibration(ProceduralWeaponAnimation proceduralWeaponAnimation, bool calibrate)
    {
    }

    public float UpdatePossibleTilt(ProceduralWeaponAnimation proceduralWeaponAnimation, float smoothedCharacterMovementSpeed,
        float smoothedPoseLevel)
    {
        var a = proceduralWeaponAnimation.TiltBlender.Value;
        return a < 1.0 ? Mathf.Max(a, ProceduralWeaponAnimation.GClass2597.GetValue(smoothedCharacterMovementSpeed, smoothedPoseLevel)) : a;
    }
}

public class ThirdPersonView : MonoBehaviour
{
    public Player localPlayer;

    private bool _thirdPersonEnabled;
    private CameraPositionEnum _cameraPosition;
    private int _cameraStance;
    
    private bool _aimFlag;
    private bool _sprintFlag;
    
    private SharedGameSettingsClass _gameSettings;
    private static readonly CustomAnimStrategy CustomAnimStrategy = new();

    public void Awake()
    {
        _thirdPersonEnabled = Plugin.PointOfViewDefault.Value == PointOfViewEnum.ThirdPerson;
        _cameraPosition = Plugin.CameraPositionDefault.Value;
        _cameraStance = (int)Plugin.CameraStanceDefault.Value;

        _gameSettings = Singleton<SharedGameSettingsClass>.Instance;
    }

    public void Update()
    {
        HandleInputs();

        var handsController = localPlayer.HandsController as Player.ItemHandsController;
        if (handsController == null || localPlayer.CameraPosition == null)
            return;
        
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
                switch (handsController.IsAiming)
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
                switch (handsController.IsAiming)
                {
                    case true when !_aimFlag:
                        _cameraPosition = CameraPositionEnum.Shoulder;
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 0.025f);
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
                switch (handsController.IsAiming)
                {
                    case true when !_aimFlag:
                        localPlayer.BodyAnimatorCommon.SetLayerWeight(8, 0.025f);
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
        
        _aimFlag = handsController.IsAiming;
        
        HandleThirdPerson();
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
            var firearmController = localPlayer.HandsController as Player.FirearmController;

            if (firearmController != null)
            {
                if ((localPlayer.MovementContext.LeftStanceEnabled && _cameraStance > 0f)
                    || (!localPlayer.MovementContext.LeftStanceEnabled && _cameraStance < 0f))
                    firearmController.ChangeLeftStance();
            }            
        }

        // The offset vector is passed by value, which means it's safe to modify it here
        var desiredCameraOffset = _cameraPosition switch
        {
            CameraPositionEnum.Main => ConfinementInterpolated(Plugin.CameraShoulderOffset.Value, Plugin.CameraMainOffset.Value),
            CameraPositionEnum.Shoulder => Plugin.CameraShoulderOffset.Value,
            _ => Plugin.CameraMainOffset.Value
        };
        desiredCameraOffset.x *= _cameraStance;

        if (localPlayer.IsSprintEnabled)
        {
            if (!_sprintFlag)
                AdjustFoV(15);
            desiredCameraOffset.x = 0f;
            desiredCameraOffset.z *= 2f;
            desiredCameraOffset.y *= 1.5f;
        }
        else if (_sprintFlag)
            ResetFoV();
        
        _sprintFlag = localPlayer.IsSprintEnabled;
        
        localPlayer.CameraPosition.localPosition = Vector3.Lerp(
            localPlayer.CameraPosition.localPosition, desiredCameraOffset, Time.deltaTime * Plugin.CameraSwitchSpeed.Value
        );
    }

    private Vector3 ConfinementInterpolated(Vector3 near, Vector3 far)
    {
        // TODO: Calculate the confinement score and interpolate between near and far. 
        return far;
    }

    private void UpdatePointOfView(EPointOfView value)
    {
        localPlayer.PointOfView = value;

        if (value == EPointOfView.ThirdPerson)
        {
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
            localPlayer.CameraPosition.localPosition = Vector3.zero;
        }
    }

    private void ScopeFoV()
    {
        var currentScopeIsOptic = localPlayer.ProceduralWeaponAnimation.CurrentScope.IsOptic;
        var fov = currentScopeIsOptic ? 35 : _gameSettings.Game.Settings.FieldOfView.Value + Plugin.AdsFovBasic.Value;
        SetFoV(fov);
    }
    
    private void AdjustFoV(int adjustment)
    {
        CameraClass.Instance.SetFov(_gameSettings.Game.Settings.FieldOfView.Value + adjustment, Plugin.FovChangeSpeed.Value);
    }
    
    private static void SetFoV(int fov)
    {
        CameraClass.Instance.SetFov(fov, Plugin.FovChangeSpeed.Value);
    }
    
    private void ResetFoV()
    {
        CameraClass.Instance.SetFov(_gameSettings.Game.Settings.FieldOfView.Value, Plugin.FovChangeSpeed.Value);
    }

    public void OnGUI()
    {
        if (!Plugin.CrosshairEnabled.Value || localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        var handsController = localPlayer.HandsController as Player.FirearmController;

        if (handsController == null)
            return;
        
        if (!handsController.IsAiming && Plugin.CrosshairAdsOnlyEnabled.Value)
            return;

        var ray = new Ray(handsController.CurrentFireport.position, handsController.WeaponDirection * 1f);
        if (!Physics.Raycast(ray, out var raycastHit, 100000, GClass3449.HitMask))
            return;

        var screenPosition = CameraClass.Instance.Camera.WorldPointToVisibleScreenPoint(raycastHit.point);

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