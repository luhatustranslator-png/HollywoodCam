using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EFT;
using EFT.Animations;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.Patches;

public class ProceduralWeaponAnimationLerpCameraPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ProceduralWeaponAnimation).GetMethod(nameof(ProceduralWeaponAnimation.LerpCamera));
    }

    [PatchPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(ProceduralWeaponAnimation __instance, float dt,
        float ____aimSwayStrength, Player.ValueBlender ____aimSwayBlender, Vector3 ____aimSwayDirection,
        Player.ValueBlenderDelay ____tacticalReload, Vector3 ____headRotationVec, Quaternion ____rotationOffset)
    {
        if (StaticData.LocalPlayer == null)
            return true;

        if (__instance != StaticData.LocalPlayer.ProceduralWeaponAnimation || StaticData.LocalPlayer.PointOfView != EPointOfView.ThirdPerson)
            return true;

        // This is a copy of the raw LerpCamera, but removes the position logic and only applies the rotation for headbob, recoil, etc...
        if (____aimSwayStrength > 0.0f)
        {
            var num2 = ____aimSwayBlender.Value;
            if (__instance.IsAiming && num2 > 0.0f)
                __instance.HandsContainer.SwaySpring.ApplyVelocity(____aimSwayDirection * num2);
        }

        var cameraTransform = __instance.HandsContainer.CameraTransform;
        var quaternion1 = Quaternion.Lerp(
            Quaternion.identity,
            __instance.HandsContainer.CameraAnimatedFP.localRotation * __instance.HandsContainer.CameraAnimatedTP.localRotation,
            10 * __instance.Single_1 * (1f - ____tacticalReload.Value)
        );
        var quaternion2 = Quaternion.Euler(__instance.HandsContainer.CameraRotation.Get() + ____headRotationVec);

        cameraTransform.localRotation = quaternion1 * quaternion2 * ____rotationOffset;

        __instance.method_19(dt);

        var curRecoilEffect = __instance.Shootingg.CurrentRecoilEffect;
        cameraTransform.localEulerAngles += 1.5f * (curRecoilEffect.GetCameraRotationRecoil() + curRecoilEffect.WeaponRecoilEffect.GetCameraRotationRecoil());

        return false;
    }
}