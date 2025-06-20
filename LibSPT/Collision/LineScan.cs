using UnityEngine;

namespace HollywoodCam.Collision;

public class LineScan(float length, int resolution)
{
    public Vector3 FindBestPosition(
        Transform eyeTransform, Vector3 cameraOffset, Vector3 direction, Vector3 objectivePos, float sphereCastRadius, LayerMask layerMask
        )
    {
        var eps = sphereCastRadius / 10;
        var lineStart = eyeTransform.TransformPoint(cameraOffset);
        var lineEnd = eyeTransform.TransformPoint(cameraOffset + length * direction);

        var bestScore = -1f;
        var bestPosition = Vector3.zero;
        
        float normFactor = resolution - 1;
        
        for (var i = 0; i < resolution; i++)
        {
            var probePosition = Vector3.Lerp(lineStart, lineEnd, i / normFactor);
            var result = CollisionUtils.SphereCast(objectivePos, probePosition, sphereCastRadius, layerMask);

            // We found a good enough position, bail out immediately
            if (1 - result.Score <= eps)
            {
                return result.TangentPoint;
            }

            if (result.Score <= bestScore) continue;
            
            bestScore = result.Score;
            bestPosition = result.TangentPoint;
        }
        
        return bestPosition;
    }
}