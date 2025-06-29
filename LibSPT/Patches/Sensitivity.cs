using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class FirearmControllerUpdateSensitivityPrefixPatchPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController).GetMethod(nameof(Player.FirearmController.UpdateSensitivity));
    }

    [PatchPrefix]
    // ReSharper disable InconsistentNaming
    public static bool Prefix(Player.FirearmController __instance, ref float ____aimingSens)
    {
        var localPlayer = Singleton<GameWorld>.Instance.MainPlayer;

        if (localPlayer == null || __instance != localPlayer.HandsController)
            return true;

        if (localPlayer.PointOfView != EPointOfView.ThirdPerson || !__instance.IsAiming) return true;

        ____aimingSens = Singleton<SharedGameSettingsClass>.Instance.Control.Settings.MouseAimingSensitivity * Plugin.AdsThirdPersonSensitivity.Value;
        return false;
    }
}