using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class PlayerBridgePointOfViewPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(BaseSoundPlayer.PlayerBridge).GetProperty(nameof(BaseSoundPlayer.PlayerBridge.PointOfView))?.GetGetMethod();
    }

    [PatchPrefix]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static bool Prefix(BaseSoundPlayer.PlayerBridge __instance, ref EPointOfView __result)
    {
        if (!__instance.iPlayer.IsYourPlayer)
            return true;
        
        // Force FP handling for local player to avoid sounds being muffled
        __result = EPointOfView.FirstPerson;
        return false;
    }
}
