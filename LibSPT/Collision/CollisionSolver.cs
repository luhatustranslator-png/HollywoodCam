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

public class PositionSolver(AdvectionSolver phase1, AdvectionSolver phase2)
{
    public readonly AdvectionSolver Phase1 = phase1;

    // Slow speed solver with large radius
    private Vector3 _phase1Velocity;

    // Instant speed solver with narrower radius

    public Vector3 Solve(
        Transform eyeTransform, Vector3 currentCameraOffset, Vector3 desiredCameraOffset, Vector3 eyeCameraPos, Vector3 targetPos, LayerMask eyeMask,
        LayerMask targetMask
    )
    {
        var phase1Position = Phase1.Solve(eyeTransform, desiredCameraOffset, eyeCameraPos, eyeMask);

        // Handle possible colliders between the current camera position and the desired target and position ourselves on the target side.
        var cameraPos = eyeTransform.TransformPoint(currentCameraOffset);
        if (CameraShiftCollisionAvoidance(ref cameraPos, phase1Position, eyeMask))
        {
            currentCameraOffset = eyeTransform.InverseTransformPoint(cameraPos);
        }

        var phase1Offset = eyeTransform.InverseTransformPoint(phase1Position);

        currentCameraOffset = Vector3.SmoothDamp(currentCameraOffset, phase1Offset, ref _phase1Velocity, 0.1f * Plugin.CameraChangeTime.Value);

        return currentCameraOffset;

        // var phase2Position = phase2.Solve(eyeTransform, currentCameraOffset, eyeCameraPos, targetPos, eyeMask, targetMask);
        // return eyeTransform.InverseTransformPoint(phase2Position);
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