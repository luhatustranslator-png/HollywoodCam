using EFT;
using EFT.Animations;

namespace HollywoodCam;

/// <summary>
///     This class is a copy of GClass888, which uses ApplyComplexRotation instead of ApplySimpleRotation and thus adds recoil effects to the gun.
///     The 3rd Person View GClass889 doesn't apply any recoil to the gun (or camera for that matter).
///     The following changes are made:
///     1. Skip AvoidObstacles to reduce the jank slightly and prevent the gun from being pushed away by walls.
///     2. Patch PWA.LerpCamera so that it doesn't molest the position, only the rotation. This retains headbob, lean tilt and recoil.
/// </summary>
public class CustomAnimStrategy : GInterface38
{
    private readonly GClass888 _wrapped = new();

    public void ProcessEffectors(ProceduralWeaponAnimation pwa, float deltaTime, int nFixedFrames = 1)
    {
        _wrapped.ProcessEffectors(pwa, deltaTime, nFixedFrames);
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
        // pwa.AvoidObstacles();
    }

    public void ApplyCameraTransformations(ProceduralWeaponAnimation pwa, float dt)
    {
        _wrapped.ApplyCameraTransformations(pwa, dt);
    }

    public void LateTransformations(ProceduralWeaponAnimation pwa, float dt)
    {
        _wrapped.LateTransformations(pwa, dt);
    }

    public void ApplyFovAdjustments(ProceduralWeaponAnimation pwa, Player player)
    {
        _wrapped.ApplyFovAdjustments(pwa, player);
    }

    public void ResetFovAdjustments(ProceduralWeaponAnimation proceduralWeaponAnimation, Player player)
    {
        _wrapped.ResetFovAdjustments(proceduralWeaponAnimation, player);
    }

    public void OpticCalibration(ProceduralWeaponAnimation proceduralWeaponAnimation, bool calibrate)
    {
        _wrapped.OpticCalibration(proceduralWeaponAnimation, calibrate);
    }

    public float UpdatePossibleTilt(ProceduralWeaponAnimation proceduralWeaponAnimation, float smoothedCharacterMovementSpeed, float smoothedPoseLevel)
    {
        return _wrapped.UpdatePossibleTilt(proceduralWeaponAnimation, smoothedCharacterMovementSpeed, smoothedPoseLevel);
    }
}