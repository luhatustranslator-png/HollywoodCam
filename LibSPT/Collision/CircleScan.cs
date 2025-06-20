using UnityEngine;

namespace HollywoodCam.Collision;

public class CircleScan
{
    public readonly Vector3[] Points;
    public readonly float[] Scores;

    public bool Success;
    public float CenterScore;
    public Vector3 CenterPoint;
    public Vector3 AdvectionVector;

    public readonly float SphereCastRadius;

    public readonly float Epsilon;

    public CircleScan(int pointCount, float radius = 1f, float epsilon = 1f)
    {
        Points = new Vector3[pointCount];
        Scores = new float[pointCount];

        var angleStep = 2f * Mathf.PI / pointCount;

        // Derive the distance between two points on the circle as 2*r*Sin(angleRadian/2), the radius of a single spherecast is half of this
        SphereCastRadius = radius * Mathf.Sin(angleStep / 2);

        for (var i = 0; i < pointCount; i++)
        {
            var angle = i * angleStep;
            Points[i] = new Vector3(
                radius * Mathf.Cos(angle),
                radius * Mathf.Sin(angle),
                0f
            );
            Scores[i] = 0f;
        }

        Epsilon = SphereCastRadius * epsilon;
    }

    public void Update(Transform eyeTransform, Vector3 cameraOffset, Vector3 objectivePos, LayerMask layerMask)
    {
        var cameraPos = eyeTransform.TransformPoint(cameraOffset);
        var result = SphereCastDistance(objectivePos, cameraPos, layerMask);
        CenterScore = result.Score;
        CenterPoint = result.TangentPoint;
        AdvectionVector = Vector3.zero;

        for (var i = 0; i < Points.Length; i++)
        {
            var pointOffset = Points[i];
            var pointPos = eyeTransform.TransformPoint(cameraOffset + pointOffset);
            result = SphereCastDistance(objectivePos, pointPos, layerMask);
            Scores[i] = result.Score;
            AdvectionVector += pointOffset.normalized * result.Score;
        }

        Success = AdvectionVector.magnitude < Epsilon;
    }

    protected virtual SphereCastResult SphereCastDistance(Vector3 originPos, Vector3 targetPos, LayerMask layerMask)
    {
        return CollisionUtils.SphereCast(originPos, targetPos, SphereCastRadius, layerMask);
    }
}
