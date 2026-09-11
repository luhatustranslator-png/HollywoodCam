using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace HollywoodCam.Patches;

public class BattleUIOnShowFireModePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Busca o método interno de troca de modo de disparo no FirearmController
        return AccessTools.Method(typeof(Player.FirearmController), "ShowFireMode") 
               ?? AccessTools.Method(typeof(Player.FirearmController), "method_9");
    }

    [PatchPrefix]
    public static void Prefix(Player.FirearmController __instance)
    {
        if (__instance == null || __instance.Player == null || __instance.Player != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player.FirearmController __instance)
    {
        if (__instance == null || __instance.Player == null || __instance.Player != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

public class BattleUIOnShowAmmoPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // Busca o método de checar munição no FirearmController
        return AccessTools.Method(typeof(Player.FirearmController), "CheckAmmo");
    }

    [PatchPrefix]
    public static void Prefix(Player.FirearmController __instance)
    {
        if (__instance == null || __instance.Player == null || __instance.Player != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player.FirearmController __instance)
    {
        if (__instance == null || __instance.Player == null || __instance.Player != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

