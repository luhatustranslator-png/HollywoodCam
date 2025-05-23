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

    public float UpdatePossibleTilt(ProceduralWeaponAnimation proceduralWeaponAnimation, float smoothedCharacterMovementSpeed, float smoothedPoseLevel)
    {
        var a = proceduralWeaponAnimation.TiltBlender.Value;
        return a < 1.0 ? Mathf.Max(a, ProceduralWeaponAnimation.GClass2597.GetValue(smoothedCharacterMovementSpeed, smoothedPoseLevel)) : a;
    }
}

public class ThirdPersonView : MonoBehaviour
{
    public Player localPlayer;
    private static readonly CustomAnimStrategy CustomAnimStrategy = new();

    public void Update()
    {
        if (Input.GetKeyDown(Plugin.ThirdPersonToggleKey.Value))
        {
            Plugin.ThirdPersonEnabled.Value = !Plugin.ThirdPersonEnabled.Value;
        }

        var cameraPosition = localPlayer.CameraPosition;

        if (!Plugin.ThirdPersonEnabled.Value)
        {
            if (localPlayer.PointOfView != EPointOfView.ThirdPerson) return;

            cameraPosition.localPosition = Vector3.zero;
            // localPlayer.PointOfView = EPointOfView.FirstPerson;
            UpdatePoV(EPointOfView.FirstPerson);

            return;
        }

        var handsController = localPlayer.HandsController as Player.ItemHandsController;

        if (handsController == null || cameraPosition == null)
            return;

        /*
         * Logic:
         * 1. If we are ADSing and in first person, nothing to do, bail
         * 2. If we are ADSing and in third person, switch to first person and bail
         * 2. If we are not ADSing and in first person, switch to third person and turn on recoil
         */
        switch (handsController.IsAiming)
        {
            case true when localPlayer.PointOfView == EPointOfView.FirstPerson:
                return;
            case true when localPlayer.PointOfView == EPointOfView.ThirdPerson:
                cameraPosition.localPosition = Vector3.zero;
                UpdatePoV(EPointOfView.FirstPerson);
                return;
            case false when localPlayer.PointOfView == EPointOfView.FirstPerson:
                UpdatePoV(EPointOfView.ThirdPerson);
                break;
        }

        var desiredCameraOffset = Plugin.CameraOffset.Value;
        cameraPosition.localPosition = Vector3.Lerp(cameraPosition.localPosition, desiredCameraOffset, Time.deltaTime * Plugin.CameraAdsSpeed.Value);
    }

    private void UpdatePoV(EPointOfView value)
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
            
            // Adjust the FOV
            if (Plugin.ThirdPersonFovChange.Value != 0)
            {
                CameraClass.Instance.SetFov(CameraClass.Instance.Fov + Plugin.ThirdPersonFovChange.Value, Plugin.ThirdPersonFovSpeed.Value);
                Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView.Value += Plugin.ThirdPersonFovChange.Value;                
            }
        }
        else
        {
            // Adjust the FOV
            if (Plugin.ThirdPersonFovChange.Value != 0)
            {
                CameraClass.Instance.SetFov(CameraClass.Instance.Fov - Plugin.ThirdPersonFovChange.Value, Plugin.ThirdPersonFovSpeed.Value);
                Singleton<SharedGameSettingsClass>.Instance.Game.Settings.FieldOfView.Value -= Plugin.ThirdPersonFovChange.Value;
            }
        }
    }

    public void OnGUI()
    {
        if (!Plugin.CrosshairEnabled.Value || localPlayer.PointOfView == EPointOfView.FirstPerson)
            return;

        var handsController = localPlayer.HandsController as Player.FirearmController;

        if (handsController == null)
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