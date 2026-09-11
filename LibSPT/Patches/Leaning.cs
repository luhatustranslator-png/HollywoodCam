using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class PlayerOnLeanPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Busca o método de inclinação no Player (antigo method_3 ou métodos de Lean)
        return AccessTools.Method(typeof(Player), "method_3") 
               ?? AccessTools.Method(typeof(Player), "Lean")
               ?? AccessTools.Method(typeof(Player), "SetTilt");
    }

    [PatchPostfix]
    private static void Postfix(Player __instance, float dir)
    {
        if (__instance == null || Plugin.GunStanceSync.Value != GunStanceSyncEnum.Lean || !__instance.IsYourPlayer || __instance.MovementContext == null)
            return;

        var firearmController = __instance.HandsController as Player.FirearmController;

        // Don't shoulder swap in first person ADS mode
        if (firearmController == null || (firearmController.IsAiming && __instance.PointOfView == EPointOfView.FirstPerson))
            return;

        if ((__instance.MovementContext.LeftStanceEnabled && dir > 0f) || (!__instance.MovementContext.LeftStanceEnabled && dir < 0f))
            firearmController.ChangeLeftStance();
    }
}