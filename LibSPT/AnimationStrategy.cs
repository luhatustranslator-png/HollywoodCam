using EFT;
using EFT.Animations;
using UnityEngine;

namespace HollywoodCam;

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
        // pwa.ApplySimpleRotation(dt);
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