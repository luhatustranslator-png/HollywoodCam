using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;

// ReSharper disable InconsistentNaming

namespace HollywoodCam.Patches;


public class BattleUIOnShowFireModePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController.GClass2037).GetMethod(nameof(Player.FirearmController.GClass2037.method_9));
    }

    [PatchPrefix]
    public static void Prefix(Player.FirearmController.GClass2037 __instance)
    {
        if (__instance.Player_0 != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player.FirearmController.GClass2037 __instance)
    {
        if (__instance.Player_0 != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

public class BattleUIOnShowAmmoPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController.GClass2037).GetMethod(nameof(Player.FirearmController.GClass2037.CheckAmmo));
    }

    [PatchPrefix]
    public static void Prefix(Player.FirearmController.GClass2037 __instance)
    {
        if (__instance.Player_0 != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player.FirearmController.GClass2037 __instance)
    {
        if (__instance.Player_0 != Singleton<GameWorld>.Instance.MainPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

