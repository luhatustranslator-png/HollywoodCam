using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class BaseSoundPlayerPointOfViewPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.PropertyGetter(typeof(BaseSoundPlayer.PlayerBridge), nameof(BaseSoundPlayer.PlayerBridge.PointOfView))
               ?? typeof(BaseSoundPlayer.PlayerBridge).GetProperty(nameof(BaseSoundPlayer.PlayerBridge.PointOfView))?.GetGetMethod();
    }

    [PatchPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(BaseSoundPlayer.PlayerBridge __instance, ref EPointOfView __result)
    {
        if (__instance == null || __instance.iPlayer == null || !__instance.iPlayer.IsYourPlayer)
            return true;
        
        // Force FP handling for local player to avoid sounds being muffled
        __result = EPointOfView.FirstPerson;
        return false;
    }
}
