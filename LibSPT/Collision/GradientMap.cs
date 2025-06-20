using UnityEngine;

namespace HollywoodCam.Collision;

public class GradientMap
{
    public readonly Vector3[] Points;
    public readonly float[] Scores;
    public readonly float[] Gradients;

    public bool Success;
    public float CenterScore;
    public Vector3 CenterPoint;
    public float AggregateGradient;
    public Vector3 AdvectionVector;

    public readonly float SphereCastRadius;

    private readonly float _epsAdvection;
    private readonly float _epsGradient;

    public GradientMap(int pointCount, float radius = 1f, float epsAdvection = 0.01f, float epsGradient = 0.05f)
    {
        Points = new Vector3[pointCount];
        Scores = new float[pointCount];
        Gradients = new float[pointCount];

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
            Gradients[i] = 0f;
        }

        _epsAdvection = epsAdvection;
        _epsGradient = epsGradient;
    }

    public void Update(Transform eyeTransform, Vector3 cameraOffset, Vector3 objectivePos, LayerMask layerMask)
    {
        var cameraPos = eyeTransform.TransformPoint(cameraOffset);
        var result = SphereCastDistance(cameraPos, objectivePos, layerMask);
        CenterScore = result.Score;
        CenterPoint = result.TangentPoint;
        AdvectionVector = Vector3.zero;
        AggregateGradient = 0f;

        for (var i = 0; i < Points.Length; i++)
        {
            var pointOffset = Points[i];
            var pointPos = eyeTransform.TransformPoint(cameraOffset + pointOffset);
            result = SphereCastDistance(pointPos, objectivePos, layerMask);
            var score = Scores[i] = result.Score;
            var gradient = Gradients[i] = score - CenterScore;
            AdvectionVector += pointOffset * gradient;
            AggregateGradient += Mathf.Abs(gradient);
        }

        AggregateGradient /= Points.Length;

        Success = AdvectionVector.magnitude < _epsAdvection || AggregateGradient < _epsGradient;
    }

    protected virtual SphereCastResult SphereCastDistance(Vector3 originPos, Vector3 targetPos, LayerMask layerMask)
    {
        return CollisionUtils.SphereCast(originPos, targetPos, SphereCastRadius, layerMask);
    }
}

public class GradientMapSwap(int pointCount, float radius = 1, float epsAdvection = 0.01f, float epsGradient = 0.05f)
    : GradientMap(pointCount, radius, epsAdvection, epsGradient)
{
    protected override SphereCastResult SphereCastDistance(Vector3 originPos, Vector3 targetPos, LayerMask layerMask)
    {
        // Swap target and origin for e.g. generating collision fields for cameras.
        // We want to maximize the distance from the target to the camera
        return CollisionUtils.SphereCast(targetPos, originPos, SphereCastRadius, layerMask);
    }
}