using System.Net;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.Patches;

/*
 * This fvckery doth come as a svrprise.
 *
 * It's needed because:
 * 1. Some deeply nested code that checks that we are in 1st person before showing the ammo counter or fire mode texts.
 * We temporarily make the game believe the local player is in first person, even if it's not.
 * 2. Animation logic around PlayerBones and the thirdPersonAuthority variables have to be tricked that they are in first person so that the hand
 * doesn't go completely haywire during movement and that we can properly do left stance.
 */
public static class PlayerPoVFuckery
{
    public static bool OverridePoV;
}

public class PlayerPointOfViewPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetProperty(nameof(Player.PointOfView))?.GetGetMethod();
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(Player __instance, ref EPointOfView __result)
    {
        if (!PlayerPoVFuckery.OverridePoV || __instance != Singleton<GameWorld>.Instance.MainPlayer) return true;

        __result = EPointOfView.FirstPerson;
        return false;
    }
}

public class PlayerVisualPassPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.VisualPass));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Player __instance)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;

        if (__instance != localPlayer || localPlayer.ProceduralWeaponAnimation.IsMountedState)
            return;

        PlayerPoVFuckery.OverridePoV = true;
    }

    [PatchFinalizer]
    // ReSharper disable once InconsistentNaming
    public static void Finalizer(Player __instance)
    {
        if (__instance != Singleton<GameWorld>.Instance.MainPlayer)
            return;

        PlayerPoVFuckery.OverridePoV = false;
    }
}

public class PlayerShotReactionsPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.ShotReactions));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        // We just loop through here and manually molest the HitPoint.force for each hitpoint in the recoil list. This should allow us to rotate & amplify the force
        foreach (var hitPoint in __instance.HitReaction.boneHitPoints)
        {
            hitPoint.force *= Plugin.FlinchScale.Value;
        }

        foreach (var hitPoint in __instance.HitReaction.effectorHitPoints)
        {
            hitPoint.force *= Plugin.FlinchScale.Value;
        }
    }
}

public class PlayerConstructorPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [], null);
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        Traverse.Create(__instance).Field("_fbbikCooldown").SetValue(4f);
        var val = Traverse.Create(__instance).Field("_fbbikCooldown").GetValue<float>();
        Plugin.Log.LogInfo($"IK Cooldown: {val}");
    }
}

public class PlayerOnLeanPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.method_3));
    }

    [PatchPostfix]
    private static void Postfix(Player __instance, float dir)
    {
        if (Plugin.GunStanceSync.Value != GunStanceSyncEnum.Lean || !__instance.IsYourPlayer || __instance.MovementContext == null)
            return;

        var firearmController = __instance.HandsController as Player.FirearmController;

        // Don't shoulder swap in first person ADS mode
        if (firearmController == null || (firearmController.IsAiming && __instance.PointOfView == EPointOfView.FirstPerson))
            return;

        if ((__instance.MovementContext.LeftStanceEnabled && dir > 0f) || (!__instance.MovementContext.LeftStanceEnabled && dir < 0f))
            firearmController.ChangeLeftStance();
    }
}

public class PlayerInitPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.Init));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var tpView = __instance.gameObject.AddComponent<ThirdPersonView>();
        tpView.localPlayer = __instance;

        // Disables the jitter when rotating on the trunk
        if (!Plugin.ShimmyEnabled.Value)
            __instance.TrunkRotationLimit = 0f;
    }
}

public class PlayerDisposePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.Dispose));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var tpView = __instance.gameObject.GetComponent<ThirdPersonView>();
        Object.DestroyImmediate(tpView);
    }
}