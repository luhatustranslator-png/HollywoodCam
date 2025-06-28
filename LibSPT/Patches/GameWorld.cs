using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

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
        var tpView = __instance.MainPlayer.gameObject.AddComponent<ThirdPersonView>();
        tpView.localPlayer = __instance.MainPlayer;

        // Disables the jitter when rotating on the trunk
        if (!Plugin.ShimmyEnabled.Value)
            __instance.MainPlayer.TrunkRotationLimit = 0f;
        
        Plugin.Log.LogInfo("Third Person Camera & Player Initialized");
    }
}
