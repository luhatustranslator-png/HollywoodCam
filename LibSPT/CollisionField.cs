using UnityEngine;

namespace HollywoodCam;

public class CollisionField
{
    public readonly Vector3[] Points;
    public readonly float[] Values;
    public readonly float[] Gradients;

    public bool Success;
    public float CenterValue;
    public float AggregateGradient;
    public Vector3 AdvectionVector;

    public readonly float SphereCastRadius;

    private readonly float _epsAdvection;
    private readonly float _epsGradient;

    public CollisionField(int pointCount, float radius = 1f, float epsAdvection = 0.01f, float epsGradient = 0.05f)
    {
        Points = new Vector3[pointCount];
        Values = new float[pointCount];
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
            Values[i] = 0f;
            Gradients[i] = 0f;
        }

        _epsAdvection = epsAdvection;
        _epsGradient = epsGradient;
    }

    public void Update(Transform cameraTransform, Vector3 rootPos, Vector3 targetPos, LayerMask rootMask, LayerMask targetMask)
    {
        var cameraPos = cameraTransform.position;

        CenterValue = (SphereCastDistance(rootPos, cameraPos, rootMask) + SphereCastDistance(targetPos, cameraPos, targetMask)) / 2;
        AdvectionVector = Vector3.zero;
        AggregateGradient = 0f;

        for (var i = 0; i < Points.Length; i++)
        {
            var pointOffset = Points[i];
            var pointPos = cameraTransform.TransformPoint(pointOffset);
            var value = Values[i] = (SphereCastDistance(rootPos, pointPos, rootMask) + SphereCastDistance(targetPos, pointPos, targetMask)) / 2;
            var gradient = Gradients[i] = value - CenterValue;
            AdvectionVector += pointOffset * gradient;
            AggregateGradient += Mathf.Abs(gradient);
        }

        AggregateGradient /= Points.Length;

        Success = AdvectionVector.magnitude < _epsAdvection || AggregateGradient < _epsGradient;
    }

    private float SphereCastDistance(Vector3 targetPos, Vector3 cameraPos, LayerMask layerMask)
    {
        var aimVector = cameraPos - targetPos;
        var aimVectorMagnitude = aimVector.magnitude;

        if (aimVectorMagnitude == 0)
            return 0f;

        if (!Physics.SphereCast(targetPos, SphereCastRadius, aimVector.normalized, out var hitInfo, aimVectorMagnitude, layerMask))
            return 1f;

        var tangentPoint = Geometry.ClosestPointOnLine(targetPos, cameraPos, hitInfo.point);

        return (tangentPoint - targetPos).magnitude / aimVectorMagnitude;
    }
}