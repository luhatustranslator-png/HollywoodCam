using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class PlayerShotReactionsPostfixPatch : ModulePatch
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

public class PlayerConstructorPostfixPatch : ModulePatch
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

public class SlotViewChangedPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(PlayerBody.GClass2119).GetMethod(nameof(PlayerBody.GClass2119.method_4));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(PlayerBody.GClass2119 __instance)
    {
        for (var i = 0; i < __instance.Renderers.Length; i++)
        {
            __instance.Renderers[i].enabled = true;
        }
    }
}