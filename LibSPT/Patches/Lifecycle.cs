using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam.Patches;

public class GameWorldStartedPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        var tpView = Singleton<ThirdPersonView>.Instance = __instance.MainPlayer.gameObject.AddComponent<ThirdPersonView>();
        tpView.localPlayer = __instance.MainPlayer;

        // Disables the jitter when rotating on the trunk
        if (!Plugin.ShimmyEnabled.Value)
            __instance.MainPlayer.TrunkRotationLimit = 0f;
        
        Plugin.Log.LogInfo("Third Person Camera & Player Initialized");
    }
}

public class PlayerDisposePrefixPatch : ModulePatch
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

        var tpView = __instance.gameObject.GetComponent<ThirdPersonView>();

        if (tpView == null)
        {
            Plugin.Log.LogInfo("Player.Dispose: ThirdPersonView has already been destroyed");
            return;
        }

        Singleton<ThirdPersonView>.Release(tpView);
        Object.DestroyImmediate(tpView);
        Plugin.Log.LogInfo("Player.Dispose: ThirdPersonView destroyed successfully");
    }
}

public class PlayerOnDeadPrefixPatch : ModulePatch
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

        var tpView = __instance.gameObject.GetComponent<ThirdPersonView>();

        if (tpView == null)
        {
            Plugin.Log.LogInfo("Player.OnDead: ThirdPersonView has already been destroyed");
            return;
        }

        Singleton<ThirdPersonView>.Release(tpView);
        Object.DestroyImmediate(tpView);
        Plugin.Log.LogInfo("Player.OnDead: ThirdPersonView destroyed");
    }
}