using UnityEngine;

namespace HollywoodCam;

public static class Geometry
{
    public static Vector3 ClosestPointOnLine(Vector3 origin, Vector3 target, Vector3 point)
    {
        var vec1 = point - origin;
        var vec2 = (target - origin).normalized;

        var d = Vector3.Distance(origin, target);
        var t = Vector3.Dot(vec2, vec1);

        if (t <= 0) 
            return origin;

        if (t >= d) 
            return target;
 
        var vec3 = vec2 * t;

        return origin + vec3;
    }
}