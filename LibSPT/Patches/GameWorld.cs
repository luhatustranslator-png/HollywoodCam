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
        // Don't bother in the hideout
        if (__instance is HideoutGameWorld)
            return;

        var tpView = __instance.gameObject.AddComponent<ThirdPersonView>();
        tpView.localPlayer = __instance.MainPlayer;
    }
}