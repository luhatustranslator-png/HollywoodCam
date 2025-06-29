using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

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