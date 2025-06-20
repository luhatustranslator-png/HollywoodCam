using System.Diagnostics.CodeAnalysis;
using HollywoodCam.Helpers;
using UnityEngine;

namespace HollywoodCam.Collision;

public static class CollisionDebug
{
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static void DrawCollisionFieldInfo(GradientMap gradientMap)
    {
        var rect = DebugUI.Label(new Vector2(50, 50), "******************************************", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Success: {gradientMap.Success}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Ctr Value: {gradientMap.CenterScore:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Agg Grad: {gradientMap.AggregateGradient:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Adv Vec: {gradientMap.AdvectionVector} > {gradientMap.AdvectionVector.magnitude:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Radius: {gradientMap.SphereCastRadius}", centered: false);
        DebugUI.Label(new Vector2(50, rect.y + rect.height), "******************************************", centered: false);

        var center = new Vector2(Screen.width / 2, Screen.height / 2);
        
        for (var i = 0; i < gradientMap.Points.Length; i++)
        {
            var point = gradientMap.Points[i];
            var value = gradientMap.Scores[i];
            var gradient = gradientMap.Gradients[i];

            // GUI Y axis is flipped...
            DebugUI.Label(center + 500 * new Vector2(point.x, -1 * point.y), $"{value:f3}/{gradient:f3}");
        }
        
        DebugUI.DrawLine(center, center + 300 * new Vector2(gradientMap.AdvectionVector.x, -1 * gradientMap.AdvectionVector.y), 2);

    }
}