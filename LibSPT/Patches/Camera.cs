using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.CameraControl;
using HarmonyLib;
using SPT.Reflection.Patching;

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
    [SuppressMessage("ReSharper", "InconsistentNaming")]
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

        if (localPlayer == null || __instance != localPlayer || localPlayer.ProceduralWeaponAnimation.IsMountedState)
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

public class PlayerCameraControllerLateUpdatePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerCameraController).GetMethod(nameof(PlayerCameraController.LateUpdate));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(PlayerCameraController __instance)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;
        var tpvInstance = Singleton<ThirdPersonView>.Instance;
        
        if(localPlayer == null || __instance.Player != localPlayer || tpvInstance == null)
            return;
        
        tpvInstance.UpdateCamera();
    }
}

public class ProceduralWeaponAnimationSetStrategyPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Encontra dinamicamente o método SetStrategy no ProceduralWeaponAnimation sem depender de nomes GInterface
        return typeof(ProceduralWeaponAnimation)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(m => m.Name == "SetStrategy" && m.GetParameters().Length == 1);
    }

    [PatchPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static void Prefix(ProceduralWeaponAnimation __instance, ref object strategy)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;

        if (localPlayer == null || __instance != localPlayer.ProceduralWeaponAnimation || strategy == null)
            return;

        // Se a estratégia atual for de terceira pessoa nativa do jogo, substitui pela CustomAnimStrategy
        string typeName = strategy.GetType().Name;
        if (typeName.Contains("ThirdPerson") || typeName.Contains("GClass908") || !typeName.Contains("FirstPerson"))
        {
            if (StaticData.CustomAnimStrategy != null)
            {
                strategy = StaticData.CustomAnimStrategy;
            }
        }
    }
}