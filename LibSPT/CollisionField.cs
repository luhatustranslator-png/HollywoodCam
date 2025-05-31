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

    public void Update(Transform cameraTransform, Vector3 eyeCameraPos, Vector3 targetPos, LayerMask eyeMask, LayerMask targetMask)
    {
        var cameraPos = cameraTransform.position;
        /*
         * Two casts:
         * 1. Eye camera position to the desired camera position. Determines how far we can move the camera back before we hit something.
         * 2. Desired camera position to the target position. Determines how far we can see towards the target before we hit something.
         *    This difference is important, we want to move the camera as far back as possible and we want to maximize how far we see to the target!
         */
        CenterValue = (SphereCastDistance(eyeCameraPos, cameraPos, eyeMask) + SphereCastDistance(cameraPos, targetPos, targetMask)) / 2;
        AdvectionVector = Vector3.zero;
        AggregateGradient = 0f;

        for (var i = 0; i < Points.Length; i++)
        {
            var pointOffset = Points[i];
            var pointPos = cameraTransform.TransformPoint(pointOffset);
            var value = Values[i] = (SphereCastDistance(eyeCameraPos, pointPos, eyeMask) + SphereCastDistance(pointPos, targetPos, targetMask)) / 2;
            var gradient = Gradients[i] = value - CenterValue;
            AdvectionVector += pointOffset * gradient;
            AggregateGradient += Mathf.Abs(gradient);
        }

        AggregateGradient /= Points.Length;

        Success = AdvectionVector.magnitude < _epsAdvection || AggregateGradient < _epsGradient;
    }

    private float SphereCastDistance(Vector3 originPos, Vector3 targetPos, LayerMask layerMask)
    {
        var aimVector = targetPos - originPos;
        var aimVectorMagnitude = aimVector.magnitude;

        if (aimVectorMagnitude == 0)
            return 0f;

        if (!Physics.SphereCast(originPos, SphereCastRadius, aimVector.normalized, out var hitInfo, aimVectorMagnitude, layerMask))
            return 1f;

        var tangentPoint = Geometry.ClosestPointOnLine(originPos, targetPos, hitInfo.point);

        // Square root to concentrate most of the effect to low values and react less to differences between higher values
        return Mathf.Sqrt((tangentPoint - originPos).magnitude / aimVectorMagnitude);
    }
}