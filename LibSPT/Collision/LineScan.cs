using UnityEngine;

namespace HollywoodCam.Collision;

public class LineScan(float length, int resolution)
{
    public Vector3 FindBestPosition(Transform eyeTransform, Vector3 cameraOffset, Vector3 direction, Vector3 eyeCameraPos, Vector3 targetPos,
        float eyeCameraSphereRadius, float targetSphereRadius, LayerMask eyeMask, LayerMask targetMask)
    {
        var eps = (eyeCameraSphereRadius + targetSphereRadius) / 10;
        var lineStart = eyeTransform.TransformPoint(cameraOffset);
        var lineEnd = eyeTransform.TransformPoint(cameraOffset + length * direction);

        var bestScore = -1f;
        var bestPosition = Vector3.zero;
        
        float normFactor = resolution - 1;
        
        for (var i = 0; i < resolution; i++)
        {
            var probePosition = Vector3.Lerp(lineStart, lineEnd, i / normFactor);
            
            var eyeCameraCollision = CollisionUtils.SphereCast(eyeCameraPos, probePosition, eyeCameraSphereRadius, eyeMask);
            // var targetCollision = CollisionUtils.SphereCast(probePosition, targetPos, targetSphereRadius, targetMask);
            
            // var score = (eyeCameraCollision.Score + targetCollision.Score) / 2;
            var score = eyeCameraCollision.Score;

            // We found a good enough position, bail out immediately
            if (1 - score <= eps)
            {
                return probePosition;
            }

            if (score <= bestScore) continue;
            
            bestScore = score;
            bestPosition = eyeCameraCollision.TangentPoint;
        }
        
        return bestPosition;
    }
}