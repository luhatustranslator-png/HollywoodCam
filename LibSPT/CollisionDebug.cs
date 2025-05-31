using System.Diagnostics.CodeAnalysis;
using HollywoodCam.Helpers;
using UnityEngine;

namespace HollywoodCam;

public static class CollisionDebug
{
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static void DrawCollisionFieldInfo( CollisionField collisionField)
    {
        var rect = DebugUI.Label(new Vector2(50, 50), "******************************************", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Success: {collisionField.Success}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Ctr Value: {collisionField.CenterValue:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Agg Grad: {collisionField.AggregateGradient:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Adv Vec: {collisionField.AdvectionVector} > {collisionField.AdvectionVector.magnitude:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Radius: {collisionField.SphereCastRadius}", centered: false);
        DebugUI.Label(new Vector2(50, rect.y + rect.height), "******************************************", centered: false);

        var center = new Vector2(Screen.width / 2, Screen.height / 2);
        
        for (var i = 0; i < collisionField.Points.Length; i++)
        {
            var point = collisionField.Points[i];
            var value = collisionField.Values[i];
            var gradient = collisionField.Gradients[i];

            // GUI Y axis is flipped...
            DebugUI.Label(center + 500 * new Vector2(point.x, -1 * point.y), $"{value:f3}/{gradient:f3}");
        }
        
        DebugUI.DrawLine(center, center + 300 * new Vector2(collisionField.AdvectionVector.x, -1 * collisionField.AdvectionVector.y), 2);

    }
}