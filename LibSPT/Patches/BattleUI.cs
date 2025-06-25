using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

// ReSharper disable InconsistentNaming

namespace HollywoodCam.Patches;


public class BattleUIOnShowFireModePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController.GClass1806).GetMethod(nameof(Player.FirearmController.GClass1806.method_9));
    }

    [PatchPrefix]
    public static void Prefix(Player ___player_0)
    {
        if (___player_0 != StaticData.LocalPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player ___player_0)
    {
        if (___player_0 != StaticData.LocalPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

public class BattleUIOnShowAmmoPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.FirearmController.GClass1806).GetMethod(nameof(Player.FirearmController.GClass1806.CheckAmmo));
    }

    [PatchPrefix]
    public static void Prefix(Player ___player_0)
    {
        if (___player_0 != StaticData.LocalPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = true;
    }
    
    [PatchFinalizer]
    public static void Finalizer(Player ___player_0)
    {
        if (___player_0 != StaticData.LocalPlayer)
            return;
        
        PlayerPoVFuckery.OverridePoV = false;
    }
}

