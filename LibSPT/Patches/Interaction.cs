using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.Patches;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class GameWorldFindInteractablePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.FindInteractable));
    }

    [PatchPrefix]
    public static bool Prefix(Ray ray, out RaycastHit hit, ref GameObject __result)
    {
        hit = default;
        var maxDistance = Mathf.Max(
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE,
            EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE + EFTHardSettings.Instance.BEHIND_CAST
        );

        // Busca o método de SphereCast dinamicamente se a classe do física original tiver mudado
        var physicsClass = AccessTools.TypeByName("EFTPhysicsClass") ?? typeof(Physics);
        var sphereCastMethod = AccessTools.Method(physicsClass, "SphereCast", new Type[] { 
            typeof(Ray), typeof(float), typeof(RaycastHit).MakeByRefType(), typeof(float), typeof(LayerMask) 
        });

        bool didHit = false;
        if (sphereCastMethod != null)
        {
            object[] parameters = new object[] { ray, Plugin.InteractionRayRadius.Value, null, maxDistance, GameWorld.InteractiveLootMaskWPlayer };
            didHit = (bool)sphereCastMethod.Invoke(null, parameters);
            if (didHit) hit = (RaycastHit)parameters[2];
        }
        else
        {
            didHit = Physics.SphereCast(ray, Plugin.InteractionRayRadius.Value, out hit, maxDistance, GameWorld.InteractiveLootMaskWPlayer);
        }

        var gameObject = didHit ? hit.collider.gameObject : null;
        __result = gameObject != null && !Physics.Linecast(ray.origin, hit.point, GameWorld.LootMaskObstruction) ? gameObject : null;

        return false;
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class PlayerInteractionRayPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetProperty(nameof(Player.InteractionRay))?.GetGetMethod();
    }

    [PatchPrefix]
    public static bool Prefix(Player __instance, ref Ray __result)
    {
        if (__instance == null 
            || !__instance.IsYourPlayer
            || CameraClass.Instance == null
            || CameraClass.Instance.Camera == null
            || __instance.PlayerBody == null
            || __instance.PlayerBody.PointOfView != EPointOfView.ThirdPerson)
            return true;

        var cameraTransform = CameraClass.Instance.Camera.transform;
        __result = new Ray(cameraTransform.position, cameraTransform.forward);
        return false;
    }
}