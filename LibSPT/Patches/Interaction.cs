using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.Patches;

public class GameWorldFindInteractablePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.FindInteractable));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(Ray ray, out RaycastHit hit, ref GameObject __result)
    {
        var maxDistance = Mathf.Max(
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE,
            EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE + EFTHardSettings.Instance.BEHIND_CAST
        );
        var gameObject = EFTPhysicsClass.SphereCast(ray, 0.25f, out hit, maxDistance, GameWorld.InteractiveLootMaskWPlayer) ? hit.collider.gameObject : null;
        __result = gameObject != null && !Physics.Linecast(ray.origin, hit.point, GameWorld.LootMaskObstruction) ? gameObject : null;
        
        return false;
    }
}

public class PlayerInteractionRaycastPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.InteractionRaycast));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Player __instance, Transform ____playerLookRaycastTransform)
    {
        Plugin.Log.LogInfo($"POV: {__instance.PointOfView} WPos: {____playerLookRaycastTransform.position} LPos: {____playerLookRaycastTransform.localPosition} Rot: {____playerLookRaycastTransform.rotation}");
        // Plugin.Log.LogInfo($"WPos: {CameraClass.Instance.Camera.transform.position} LPos: {CameraClass.Instance.Camera.transform.localPosition}");
    }
}