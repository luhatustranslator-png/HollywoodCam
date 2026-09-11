using System;
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
        if (__instance == null || !__instance.IsYourPlayer || __instance.HitReaction == null)
            return;

        // Multiplica a força das reações de tiro nos pontos de impacto do corpo
        if (__instance.HitReaction.boneHitPoints != null)
        {
            foreach (var hitPoint in __instance.HitReaction.boneHitPoints)
            {
                hitPoint.force *= Plugin.FlinchScale.Value;
            }
        }

        if (__instance.HitReaction.effectorHitPoints != null)
        {
            foreach (var hitPoint in __instance.HitReaction.effectorHitPoints)
            {
                hitPoint.force *= Plugin.FlinchScale.Value;
            }
        }
    }
}

public class PlayerConstructorPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Pega qualquer construtor de instância da classe Player
        var constructors = typeof(Player).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        return constructors.Length > 0 ? constructors[0] : null;
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (__instance == null) return;

        var fbbikField = Traverse.Create(__instance).Field("_fbbikCooldown");
        if (fbbikField.FieldExists())
        {
            fbbikField.SetValue(4f);
            var val = fbbikField.GetValue<float>();
            Plugin.Log.LogInfo($"IK Cooldown: {val}");
        }
    }
}

public class SlotViewChangedPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Busca o método de atualização do renderizador de slots no PlayerBody
        return AccessTools.Method(typeof(PlayerBody.EquipmentSlotClass), "method_4")
               ?? AccessTools.Method(typeof(PlayerBody.EquipmentSlotClass), "UpdateRenderers")
               ?? AccessTools.Method(typeof(PlayerBody.EquipmentSlotClass), "SetSlotView");
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(PlayerBody.EquipmentSlotClass __instance)
    {
        if (__instance == null || __instance.Renderers == null) return;

        for (var i = 0; i < __instance.Renderers.Length; i++)
        {
            if (__instance.Renderers[i] != null)
            {
                __instance.Renderers[i].enabled = true;
            }
        }
    }
}