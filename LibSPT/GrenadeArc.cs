using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using SPT.Reflection.Patching;
using UnityEngine;
using Random = UnityEngine.Random;

namespace HollywoodCam;

public class GrenadeArc : MonoBehaviour
{
    public Player localPlayer;
    public GrenadeThrow GrenadeThrow;

    private LineRenderer _line;
    private Vector3[] _positions;

    private float _gravity;
    private const float GrenadeMass = 0.6f;
    private const float LinearDrag = 0.1f;

    public void Awake()
    {
        // Add a LineRenderer component
        _line = gameObject.AddComponent<LineRenderer>();

        // Set the material
        _line.material = new Material(Shader.Find("Sprites/Default"));

        // Disable lighting
        // _line.generateLightingData = false;
        _line.numCapVertices = 1;
        _line.numCornerVertices = 3;

        // Set the color
        _line.startColor = new Color(0, 1, 0, 0f);
        _line.endColor = new Color(1, 0, 0, 0.75f);

        // Set the width
        _line.startWidth = 0.1f;
        _line.endWidth = 0.05f;

        _positions = new Vector3[200];
        _gravity = -Physics.gravity.y;
    }

    public void Update()
    {
        if (localPlayer == null || !localPlayer.HealthController.IsAlive || !Plugin.GrenadeArcEnabled.Value)
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

        // NB: The grenade weighs 0.6 at the moment that it's initialized and the force applied. The weight is set to 0.5 afterward for `reasons`.
        // Velocity is (impulse / rigidbody.mass) * Time.fixedDeltaTime, assuming that the impulse was scaled up to 1 second unit by dividing by fixedDeltaTime
        // Since we don't do the division by fixedDeltaTime in CalculateGrenadeThrow, we don't need to multiply here.
        // NB: in AddForce with Impulse mode, unity will assume that the impulse is *per fixed frame time* and will then scale it up to a whole second
        // if we observe the accumulated forces on the rigidbody.
        // Drag is implemented as Mathf.Clamp01(1f - rigidbody.drag * Time.fixedDeltaTime) ran every fixed delta frame
        // The drag itself seems to be set to 0.1f
        var throwVelocity = GrenadeThrow.ThrowForce / GrenadeMass;
        var intervalDistance = Plugin.GrenadeArcResolution.Value;
        var maxDistance = Plugin.GrenadeArcDistance.Value;
        GetBallisticArcWithLinearDrag(
            _positions, GrenadeThrow.ThrowPosition, throwVelocity, intervalDistance, maxDistance, _gravity, LinearDrag, out var positionCount
        );

        _line.positionCount = positionCount;
        _line.SetPositions(_positions);
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

    private static void GetBallisticArcWithLinearDrag(
        Vector3[] positions,
        Vector3 startPosition,
        Vector3 initialVelocity,
        float intervalDistance,
        float maxDistance,
        float gravity,
        float linearDragCoefficient,
        out int positionCount
    )
    {
        var i = 0;

        positions[i] = startPosition;
        i++;

        // For linear drag, the analytical solution is:
        // x(t) = x0 + (v0x/k) * (1 - e^(-k*t))
        // y(t) = y0 + (1/k) * ((v0y + g/k) * (1 - e^(-k*t)) - g*t)
        // z(t) = z0 + (v0z/k) * (1 - e^(-k*t))

        var k = linearDragCoefficient;
        if (k < 0.0001f) k = 0.0001f; // Avoid division by zero

        var v0X = initialVelocity.x;
        var v0Y = initialVelocity.y;
        var v0Z = initialVelocity.z;

        // Calculate horizontal speed for distance tracking
        var horizontalSpeed = Mathf.Sqrt(v0X * v0X + v0Z * v0Z);
        if (horizontalSpeed < 0.001f)
        {
            positionCount = i;
            return;
        }

        // Maximum reachable horizontal distance due to drag
        var maxReachableDistance = horizontalSpeed / k;

        var currentDistance = intervalDistance;

        while (currentDistance <= maxDistance && currentDistance < maxReachableDistance * 0.999f && i < positions.Length)
        {
            // Solve for time given horizontal distance
            // d = sqrt((v0x/k)^2 + (v0z/k)^2) * (1 - e^(-k*t))
            // t = -ln(1 - d*k/sqrt(v0x^2 + v0z^2)) / k
            var ratio = currentDistance * k / horizontalSpeed;
            var t = -Mathf.Log(1f - ratio) / k;

            // Calculate position using analytical solution
            var dragTerm = Mathf.Exp(-k * t);

            var x = startPosition.x + (v0X / k) * (1f - dragTerm);
            var y = startPosition.y + (1f / k) * ((v0Y + gravity / k) * (1f - dragTerm) - gravity * t);
            var z = startPosition.z + (v0Z / k) * (1f - dragTerm);

            positions[i] = new Vector3(x, y, z);
            currentDistance += intervalDistance;
            i++;
        }

        positionCount = i;
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
        Plugin.Log.LogInfo(
            $"Grenade method10: {__result} RB: {rb} RB Mass: {rb?.mass} Force: {rb?.GetAccumulatedForce()} FM: {rb?.GetAccumulatedForce().magnitude} Drag: {rb?.drag}"
        );

        // Plugin.Log.LogInfo(Environment.StackTrace);
    }
}

public class TestPatch6 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GrenadeFactoryClass).GetMethod(nameof(GrenadeFactoryClass.Create));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Grenade __result)
    {
        var rb = __result.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo(
            $"Grenade Create: {__result} RB: {rb} RB Mass: {rb?.mass} Force: {rb?.GetAccumulatedForce()} FM: {rb?.GetAccumulatedForce().magnitude} Drag: {rb?.drag}"
        );

        // Plugin.Log.LogInfo(Environment.StackTrace);
    }
}


public class TestPatch3 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.BaseGrenadeHandsController).GetMethod(nameof(Player.BaseGrenadeHandsController.method_9));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Player.BaseGrenadeHandsController __instance, float forcePower, float lowHighThrow)
    {
        Plugin.Log.LogInfo($"Method9 forcePower: {forcePower} lowHighThrow: {lowHighThrow}");
    }
}

public class TestPatch4 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.BaseGrenadeHandsController).GetMethod(nameof(Player.BaseGrenadeHandsController.vmethod_2));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Player.BaseGrenadeHandsController __instance, Vector3 force)
    {
        Plugin.Log.LogInfo($"vmethod_2 force: {force} {force.magnitude}");
    }
}

public class TestPatch5 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        
        return typeof(Grenade).GetMethod(
            nameof(Grenade.Init),
            types: [typeof(GrenadeSettings), typeof(string), typeof(ThrowWeapItemClass), typeof(float), typeof(ISharedBallisticsCalculator), typeof(bool)]
            );
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Grenade __instance)
    {
        var rb = __instance.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo(
            $"Grenade Init Prefix RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.GetAccumulatedForce()} VM: {rb?.GetAccumulatedForce().magnitude}");
    }
    
    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Grenade __instance)
    {
        var rb = __instance.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo(
            $"Grenade Init Postfix RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.GetAccumulatedForce()} VM: {rb?.GetAccumulatedForce().magnitude}");
    }
}

public class TestPatch7 : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Grenade).GetMethod(nameof(Grenade.SetThrowForce));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Prefix(Grenade __instance)
    {
        var rb = __instance.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo(
            $"Grenade Init Prefix RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.GetAccumulatedForce()} VM: {rb?.GetAccumulatedForce().magnitude}");
    }
    
    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Grenade __instance)
    {
        var rb = __instance.GetComponent<Rigidbody>();
        Plugin.Log.LogInfo(
            $"Grenade Init Postfix RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.GetAccumulatedForce()} VM: {rb?.GetAccumulatedForce().magnitude}");
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
        Plugin.Log.LogInfo(
            $"Grenade: {__instance} RB: {rb} RB Mass: {rb?.mass} Velocity: {rb?.velocity} VM: {rb?.velocity.magnitude} Drag: {rb?.drag}");
    }
}