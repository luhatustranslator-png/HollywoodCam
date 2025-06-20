using System.Diagnostics.CodeAnalysis;
using HollywoodCam.Helpers;
using UnityEngine;

namespace HollywoodCam.Collision;

public static class CollisionDebug
{
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static void DrawCollisionFieldInfo(GradientScan gradientScan)
    {
        var rect = DebugUI.Label(new Vector2(50, 50), "******************************************", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Success: {gradientScan.Success}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Ctr Value: {gradientScan.CenterScore:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Agg Grad: {gradientScan.AggregateGradient:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Adv Vec: {gradientScan.AdvectionVector} > {gradientScan.AdvectionVector.magnitude:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Radius: {gradientScan.SphereCastRadius}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Eps: {gradientScan.EpsGradient}/{gradientScan.EpsAdvection}", centered: false);
        DebugUI.Label(new Vector2(50, rect.y + rect.height), "******************************************", centered: false);

        var center = new Vector2(Screen.width / 2, Screen.height / 2);
        
        for (var i = 0; i < gradientScan.Points.Length; i++)
        {
            var point = gradientScan.Points[i];
            var value = gradientScan.Scores[i];
            var gradient = gradientScan.Gradients[i];

            // GUI Y axis is flipped...
            DebugUI.Label(center + 500 * new Vector2(point.x, -1 * point.y), $"{value:f3}/{gradient:f3}");
        }
        
        DebugUI.DrawLine(center, center + 300 * new Vector2(gradientScan.AdvectionVector.x, -1 * gradientScan.AdvectionVector.y), 2);

    }
    
    [SuppressMessage("ReSharper", "PossibleLossOfFraction")]
    public static void DrawCollisionFieldInfo(CircleScan scan)
    {
        var rect = DebugUI.Label(new Vector2(50, 50), "******************************************", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Success: {scan.Success}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Ctr Value: {scan.CenterScore:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"Adv Vec: {scan.AdvectionVector} > {scan.AdvectionVector.magnitude:f4}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Radius: {scan.SphereCastRadius}", centered: false);
        rect = DebugUI.Label(new Vector2(50, rect.y + rect.height), $"SpC Eps: {scan.Epsilon}", centered: false);
        DebugUI.Label(new Vector2(50, rect.y + rect.height), "******************************************", centered: false);

        var center = new Vector2(Screen.width / 2, Screen.height / 2);
        
        for (var i = 0; i < scan.Points.Length; i++)
        {
            var point = scan.Points[i];
            var value = scan.Scores[i];

            // GUI Y axis is flipped...
            DebugUI.Label(center + 500 * new Vector2(point.x, -1 * point.y), $"{value:f3}");
        }
        
        DebugUI.DrawLine(center, center + 300 * new Vector2(scan.AdvectionVector.x, -1 * scan.AdvectionVector.y), 2);
    }
}