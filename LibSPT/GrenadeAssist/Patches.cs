using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.GrenadeAssist;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class GrenadeSetThrowForcePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Grenade).GetMethod(nameof(Grenade.SetThrowForce));
    }

    [PatchPrefix]
    public static bool Prefix(Grenade __instance, Rigidbody ___Rigidbody, ref Vector3 ___Velocity)
    {
        var grenadeArc = Singleton<GrenadeArc>.Instance;

        if (!__instance.Player.iPlayer.IsYourPlayer || grenadeArc is null || !Plugin.GrenadeArcEnabled.Value)
            return true;

        __instance.transform.position = grenadeArc.GrenadeThrow.ThrowPosition;
        ___Velocity = grenadeArc.GrenadeThrow.ThrowForce;
        ___Rigidbody.AddForce(grenadeArc.GrenadeThrow.ThrowForce, ForceMode.Impulse);
        return false;
    }
}

public class GrenadeAssistGameWorldStartedPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        var grenadeArc = Singleton<GrenadeArc>.Instance = __instance.MainPlayer.gameObject.AddComponent<GrenadeArc>();
        grenadeArc.localPlayer = __instance.MainPlayer;
        Plugin.Log.LogInfo("Grenade Assist Initialized");
    }
}

public class GrenadeAssistPlayerDisposePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.Dispose));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var grenadeArc = __instance.gameObject.GetComponent<GrenadeArc>();
        if (grenadeArc == null) return;
        
        Singleton<GrenadeArc>.Release(grenadeArc);
        Object.DestroyImmediate(grenadeArc);
        Plugin.Log.LogInfo("Player.Dispose: GrenadeArc destroyed successfully");
    }
}

public class GrenadeAssistPlayerOnDeadPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.OnDead));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var grenadeArc = __instance.gameObject.GetComponent<GrenadeArc>();
        if (grenadeArc == null) return;
        
        Singleton<GrenadeArc>.Release(grenadeArc);
        Object.DestroyImmediate(grenadeArc);
        Plugin.Log.LogInfo("Player.Dispose: GrenadeArc destroyed successfully");
    }
}