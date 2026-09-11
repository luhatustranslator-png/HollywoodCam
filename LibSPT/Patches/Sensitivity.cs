using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class FirearmControllerUpdateSensitivityPrefixPatchPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player.FirearmController), nameof(Player.FirearmController.UpdateSensitivity))
               ?? AccessTools.Method(typeof(Player.FirearmController), "UpdateSensitivity");
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Player.FirearmController __instance, ref float ____aimingSens)
    {
        var localPlayer = Singleton<GameWorld>.Instance?.MainPlayer;

        if (localPlayer == null || __instance == null || __instance != localPlayer.HandsController)
            return true;

        if (localPlayer.PointOfView != EPointOfView.ThirdPerson || !__instance.IsAiming) 
            return true;

        var controlSettings = Singleton<SharedGameSettingsClass>.Instance?.Control?.Settings;
        if (controlSettings != null)
        {
            ____aimingSens = controlSettings.MouseAimingSensitivity * Plugin.AdsThirdPersonSensitivity.Value;
            return false;
        }

        return true;
    }
}