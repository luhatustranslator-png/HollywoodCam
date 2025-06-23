using UnityEngine;

namespace HollywoodCam.Collision;

public class AdvectionSolver(CircleScan circleScan, LineScan lineScan)
{
    public readonly CircleScan CircleScan = circleScan;
    public readonly LineScan LineScan = lineScan;

    public Vector3 Solve(
        Transform eyeTransform, Vector3 cameraOffset, Vector3 objectivePos, LayerMask layerMask
    )
    {
        CircleScan.Update(eyeTransform, cameraOffset, objectivePos, layerMask);

        // There's either no collision or no gradient. We grab the tangent to the central raycast as the desired position.
        if (CircleScan.Success)
        {
            return CircleScan.CenterPoint;
        }

        // var scanDir = CircleScan.AdvectionVector.normalized;
        var scanOffset = CircleScan.AdvectionVector;
        var scanRadius = CircleScan.SphereCastRadius;

        return LineScan.FindBestPosition(eyeTransform, cameraOffset, scanOffset, objectivePos, scanRadius, layerMask);
    }
}

public class PositionSolver(
    AdvectionSolver phase1,
    AdvectionSolver phase2,
    AdvectionSolver phase3,
    float phase1SmoothTime,
    float phase2SmoothTime
)
{
    public readonly AdvectionSolver Phase1 = phase1;
    public readonly AdvectionSolver Phase2 = phase2;
    public readonly AdvectionSolver Phase3 = phase3;

    // Slow speed solver with large radius. The chode solver. It's all about the girth.
    private Vector3 _phase1Velocity;

    // Fast speed solver with narrow radius. It's all about the technique.
    private Vector3 _phase2Velocity;

    public Vector3 Solve(Transform eyeTransform, Vector3 currentCameraOffset, Vector3 desiredCameraOffset, Vector3 eyePos, LayerMask eyeMask)
    {
        var phase1Offset = ApplySolver(Phase1, eyeTransform, ref currentCameraOffset, desiredCameraOffset, eyePos, eyeMask);
        currentCameraOffset = Vector3.SmoothDamp(currentCameraOffset, phase1Offset, ref _phase1Velocity,
            phase1SmoothTime / Plugin.CameraMoveSpeed.Value);

        var phase2Offset = ApplySolver(Phase2, eyeTransform, ref currentCameraOffset, currentCameraOffset, eyePos, eyeMask);
        currentCameraOffset = Vector3.SmoothDamp(currentCameraOffset, phase2Offset, ref _phase2Velocity,
            phase2SmoothTime / Plugin.CameraMoveSpeed.Value);

        var finalCameraPos = Phase3.Solve(eyeTransform, currentCameraOffset, eyePos, eyeMask);
        return eyeTransform.InverseTransformPoint(finalCameraPos);
    }

    private static Vector3 ApplySolver(
        AdvectionSolver solver, Transform eyeTransform, ref Vector3 currentCameraOffset, Vector3 desiredCameraOffset, Vector3 objectivePos,
        LayerMask layerMask
    )
    {
        var solvedPosition = solver.Solve(eyeTransform, desiredCameraOffset, objectivePos, layerMask);

        // Handle possible colliders between the current camera position and the desired target and position ourselves on the target side.
        var cameraPos = eyeTransform.TransformPoint(currentCameraOffset);
        if (CameraShiftCollisionAvoidance(ref cameraPos, solvedPosition, layerMask))
        {
            currentCameraOffset = eyeTransform.InverseTransformPoint(cameraPos);
        }

        return eyeTransform.InverseTransformPoint(solvedPosition);
    }

    private static bool CameraShiftCollisionAvoidance(ref Vector3 currentCameraPos, Vector3 targetCameraPos, LayerMask eyeMask)
    {
        var movementVector = currentCameraPos - targetCameraPos;
        var ray = new Ray(targetCameraPos, movementVector.normalized);

        var result = Physics.Raycast(ray, out var hitInfo, movementVector.magnitude, eyeMask);

        if (result)
            currentCameraPos = hitInfo.point;

        return result;
    }
}