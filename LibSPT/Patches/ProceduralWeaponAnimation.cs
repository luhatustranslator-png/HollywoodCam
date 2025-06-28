using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Comfort.Common;
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
    public static bool Prefix(ProceduralWeaponAnimation __instance, float dt, Quaternion ____cameraIdenity,
        float ____aimSwayStrength, Player.ValueBlender ____aimSwayBlender, Vector3 ____aimSwayDirection,
        Player.ValueBlenderDelay ____tacticalReload, Vector3 ____headRotationVec, Quaternion ____rotationOffset)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;
        
        if (localPlayer == null)
            return true;

        if (__instance != localPlayer.ProceduralWeaponAnimation || localPlayer.PointOfView != EPointOfView.ThirdPerson)
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
            ____cameraIdenity,
            __instance.HandsContainer.CameraAnimatedFP.localRotation * __instance.HandsContainer.CameraAnimatedTP.localRotation,
            __instance.Single_1 * (1f - ____tacticalReload.Value)
        );
        var quaternion2 = Quaternion.Euler(__instance.HandsContainer.CameraRotation.Get() + ____headRotationVec);

        cameraTransform.localRotation = quaternion1 * quaternion2 * ____rotationOffset;

        __instance.method_19(dt);

        var curRecoilEffect = __instance.Shootingg.CurrentRecoilEffect;
        cameraTransform.localEulerAngles += 1.5f * Plugin.CamShakeScale.Value *
                                            (curRecoilEffect.GetCameraRotationRecoil() +
                                             curRecoilEffect.WeaponRecoilEffect.GetCameraRotationRecoil());

        return false;
    }
}

public class ProceduralWeaponAnimationSetStrategyPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(ProceduralWeaponAnimation).GetMethod(nameof(ProceduralWeaponAnimation.SetStrategy), types: [typeof(GInterface38)]);
    }

    [PatchPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void Prefix(ProceduralWeaponAnimation __instance, ref GInterface38 strategy)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;
        
        if (localPlayer == null || __instance != localPlayer.ProceduralWeaponAnimation)
            return;

        if (strategy is GClass889)
        {
            // Hijack any attempt at using the shonky builtin 3rd person strategy
            strategy = StaticData.CustomAnimStrategy;
        }
    }
}
