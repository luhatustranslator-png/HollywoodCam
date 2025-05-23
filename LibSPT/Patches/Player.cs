using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class PlayerShotReactionsPostFixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.ShotReactions));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        // We just loop through here and manually molest the HitPoint.force for each hitpoint in the recoil list. This should allow us to rotate & amplify the force
        foreach (var hitPoint in __instance.HitReaction.boneHitPoints)
        {
            hitPoint.force *= Plugin.FlinchScale.Value;
        }

        foreach (var hitPoint in __instance.HitReaction.effectorHitPoints)
        {
            hitPoint.force *= Plugin.FlinchScale.Value;
        }
    }
}

public class PlayerConstructorPostFixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [], null);
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        Traverse.Create(__instance).Field("_fbbikCooldown").SetValue(4f);
        var val = Traverse.Create(__instance).Field("_fbbikCooldown").GetValue<float>();
        Plugin.Log.LogInfo($"IK Cooldown: {val}");
    }
}

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
