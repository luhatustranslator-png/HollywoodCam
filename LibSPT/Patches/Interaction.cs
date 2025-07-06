using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EFT;
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
        var maxDistance = Mathf.Max(
            EFTHardSettings.Instance.LOOT_RAYCAST_DISTANCE,
            EFTHardSettings.Instance.PLAYER_RAYCAST_DISTANCE + EFTHardSettings.Instance.BEHIND_CAST
        );
        var gameObject =
            EFTPhysicsClass.SphereCast(ray, Plugin.InteractionRayRadius.Value, out hit, maxDistance, GameWorld.InteractiveLootMaskWPlayer)
                ? hit.collider.gameObject
                : null;
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
        if (!__instance.IsYourPlayer || CameraClass.Instance == null || CameraClass.Instance.Camera == null || __instance.PlayerBody.PointOfView != EPointOfView.ThirdPerson)
            return true;

        var cameraTransform = CameraClass.Instance.Camera.transform;
        __result = new Ray(cameraTransform.position, cameraTransform.forward);
        return false;
    }
}