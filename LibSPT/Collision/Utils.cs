using UnityEngine;

namespace HollywoodCam.Collision;

public struct SphereCastResult(float score, Vector3 tangentPoint)
{
    public readonly float Score = score;
    public readonly Vector3 TangentPoint = tangentPoint;
}

public static class CollisionUtils
{
    public static SphereCastResult SphereCast(Vector3 originPos, Vector3 targetPos, float radius, LayerMask layerMask)
    {
        var aimVector = targetPos - originPos;
        var aimVectorMagnitude = aimVector.magnitude;

        if (aimVectorMagnitude == 0)
            return new SphereCastResult(0f, originPos);

        if (!Physics.SphereCast(originPos, radius, aimVector.normalized, out var hitInfo, aimVectorMagnitude, layerMask))
            return new SphereCastResult(1f, targetPos);

        var tangentPoint = Geometry.ClosestPointOnLine(originPos, targetPos, hitInfo.point);

        // Square root to concentrate most of the effect to low values and react less to differences between higher values
        var score = Mathf.Sqrt((tangentPoint - originPos).magnitude / aimVectorMagnitude);
        
        return new SphereCastResult(score, tangentPoint);
    }
    
}