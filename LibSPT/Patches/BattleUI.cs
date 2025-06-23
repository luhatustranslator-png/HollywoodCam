using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

// ReSharper disable InconsistentNaming

namespace HollywoodCam.Patches;

/*
 * This fvckery doth come as a svrprise.
 *
 * It's needed because some deeply nested code that checks that we are in 1st person before showing the ammo counter or fire mode texts.
 * We temporarily make the game believe the local player is in first person, even if it's not.
 */
public static class PlayerPoVFuckery
{
    public static bool OverridePoV;
}

public class BattleUIPlayerPointOfViewOverridePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetProperty(nameof(Player.PointOfView))?.GetGetMethod();
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static bool Prefix(ref EPointOfView __result)
    {
        if (!PlayerPoVFuckery.OverridePoV) return true;
        
        __result = EPointOfView.FirstPerson;
        return false;
    }
}

public class BattleUIOnShowFireModePrefixPatch : ModulePatch
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

public class BattleUIOnShowAmmoPrefixPatch : ModulePatch
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

