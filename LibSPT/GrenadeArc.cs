using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace HollywoodCam;

public class GrenadeArc : MonoBehaviour
{
    public Player localPlayer;
    public GrenadeThrow GrenadeThrow;

    private LineRenderer _line;

    public void Awake()
    {
        // Add a LineRenderer component
        _line = gameObject.AddComponent<LineRenderer>();

        // Set the material
        _line.material = new Material(Shader.Find("Sprites/Default"));

        // Disable lighting
        // _line.generateLightingData = false;

        // Set the color
        _line.startColor = new Color(0, 1, 0, 0f);
        _line.endColor = new Color(1, 0, 0, 0.75f);

        // Set the width
        _line.startWidth = 0.1f;
        _line.endWidth = 0.05f;
    }

    public void Update()
    {
        if (localPlayer == null || !localPlayer.HealthController.IsAlive)
            return;

        var grenadeHandsController = localPlayer.HandsController as Player.GrenadeHandsController;

        if (grenadeHandsController == null
            || (grenadeHandsController.CurrentOperation is not Player.GrenadeHandsController.Class1156
                && grenadeHandsController.CurrentOperation is not Player.GrenadeHandsController.Class1157))
        {
            _line.enabled = false;
            return;
        }

        if (!_line.enabled)
            _line.enabled = true;

        // Class1156 is high throw 1157 low throw
        var isLowThrow = grenadeHandsController.CurrentOperation is Player.GrenadeHandsController.Class1157;
        GrenadeThrow = CalculateGrenadeThrow(isLowThrow);
        
        // NB: The grenade weighs 0.6 at the moment that it's initialized and the force applied. The weight is set to 0.5 afterward for reasons.
        // Velocity is (impulse / rigidbody.mass) * Time.fixedDeltaTime;
        // Drag is Mathf.Clamp01(1f - rigidbody.drag * Time.fixedDeltaTime);
        _line.positionCount = 2;

        // Set the positions of the vertices
        _line.SetPosition(0, GrenadeThrow.ThrowPosition);
        _line.SetPosition(1, GrenadeThrow.ThrowPosition + 2 * GrenadeThrow.ThrowForce);
    }

    private GrenadeThrow CalculateGrenadeThrow(bool low)
    {
        // Taken from BaseGrenadeHandsController.vmethod_1
        var lowHighThrow = low ? 0.66f : 1f + (float)localPlayer.Skills.StrengthBuffThrowDistanceInc;
        var forcePower = EFTHardSettings.Instance.GrenadeForce;

        // Taken from the assignment of transform_0 in Player.BaseGrenadeHandsController
        var rootTransform = localPlayer.PlayerBones.WeaponRoot.Original;

        Vector3 direction;

        if (!(bool)localPlayer.Skills.ThrowingEliteBuff)
        {
            var handStamina = localPlayer.Physical.HandsStamina.NormalValue;
            direction = (-rootTransform.up * 5f + Mathf.Clamp01(0.5f - handStamina) * Random.onUnitSphere).normalized;
            lowHighThrow *= Mathf.Lerp(0.4f, 1f, handStamina + 0.5f);
        }
        else
            direction = -rootTransform.up;

        var force = direction * (forcePower * lowHighThrow) + localPlayer.Velocity;

        // var throwPosition = grenadeHandsController.FindThrowPosition();
        var throwPosition = localPlayer.PlayerBones.WeaponRoot.Original.position + 0.5f * direction;
        return new GrenadeThrow { ThrowPosition = throwPosition, ThrowForce = force };
    }
}

public struct GrenadeThrow
{
    public Vector3 ThrowPosition;
    public Vector3 ThrowForce;
}

public class TestPatch1 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.BaseGrenadeHandsController).GetMethod(nameof(Player.BaseGrenadeHandsController.method_10));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Grenade __result)
    {
        var rb = __result.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo($"Grenade: {__result} RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.GetAccumulatedForce()} VM: {rb?.velocity.magnitude} Drag: {rb?.drag}");
    }
}

public class TestPatch2 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Grenade).GetMethod(nameof(Grenade.LateUpdate));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Grenade __instance)
    {
        if (__instance.Player.iPlayer as Player != Singleton<GameWorld>.Instance.MainPlayer)
            return;

        var rb = __instance.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo($"Grenade: {__instance} RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.velocity} VM: {rb?.velocity.magnitude} Drag: {rb?.drag}");
    }
}