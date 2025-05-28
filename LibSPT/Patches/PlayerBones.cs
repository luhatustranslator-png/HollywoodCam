using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class PlayerBonesShiftWeaponRootPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerBones).GetMethod(nameof(PlayerBones.ShiftWeaponRoot));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(PlayerBones __instance, EPointOfView pv, ref float thirdPersonAuthority, bool inSprint)
    {
        if (!StaticData.InRaid || StaticData.LocalPlayer == null || inSprint || pv != EPointOfView.ThirdPerson)
            return;

        thirdPersonAuthority = 0f;
    }
}