# Camera Confinement Handling
## Algorithm
* Run the advection until one of the following is met:
  1. The average absolute gradient is lower than some epsilon
  2. The magnitude of the advection vector is lower than some epsilon
  3. We exhaust the iteration limit
* Then do a final spherecast at that point and relocate the camera along the ray as far back as collisions allow (all the way if no collisions).

# !!!! TODO !!!!
1. CollisionField will be adjusted to work on one target only, and make sure we support both the camera -> target and target -> camera directions.
2. We'll have a CollisionField for the eye camera and another one for the aim target (which will be smaller)
3. We calculate an advection vector for each, average them and do a line search along it using a separate (new) class
4. We find the first unoccluded point on the line, or if there isn't such, then the best position on it.
5. We do a smoothdamp to that point
6. Finally, we do a linecast to the camera position after the smoothdamp and if it's still occluded, we force push it to the unoccluded point

# Optic sight not showing
PWA.method_1 seems to be enabling/disabling optics

# Aiming
PWA.applycomplexrotation rotates the player aim, so if we want him to aim where the camera points, this is where to do it

# UI Text not showing
GamePlayerOwner.MyPlayer.ShowAmmoCountZeroingPanel shows teh zeroing panel, figure out why this is not showing.

```csharp
localPlayer.PlayerBones.ShiftWeaponRoot: seems to fuck around with the weapon root? Has a thirdPersonAuthority that seems to adjust
how much 3rd person fucks with the weapon?

This seems to be the root of the issue. Forcing the 3rd person authority to 0 here completely fixes the wiggle. It also breaks
running, so we have to do something here.
    
Maybe condition stuff on the params:

    bool armsupdated = false,
    float positionCacheValue = 0.0f,
    float leftStanceCurveValue = 0.0f,
    bool inSprint = false,
    bool inLeftStanceAnimValue = false,
    bool inLeftStanceCacheValue = false,
    bool isAiming = false,
    bool isAnimatedInteraction = false)

localPlayer.VisualPass calculates the authority


localPlayer.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = 0;
localPlayer.ProceduralWeaponAnimation.HandsContainer.HandsPosition.InputIntensity = 0;
localPlayer.ProceduralWeaponAnimation.HandsContainer.SwaySpring._dampingRatio = 1;
localPlayer.ProceduralWeaponAnimation.Breath.Intensity = 0;
localPlayer.ProceduralWeaponAnimation.HandShakeEffector._shakeIntensity = 0;
localPlayer.ProceduralWeaponAnimation.MotionReact.Intensity = 0;
localPlayer.ProceduralWeaponAnimation.Breath.HipPenalty = 0;
localPlayer.ProceduralWeaponAnimation.Walk.Intensity = 0;
localPlayer.ProceduralWeaponAnimation.Walk.Overweight = 0;

EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1000, 0));
EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1000, 0));
    
Traverse.Create(localPlayer.ProceduralWeaponAnimation).Field("_swayStrength").SetValue(0f);
Traverse.Create(localPlayer.ProceduralWeaponAnimation).Field("_displacementStr").SetValue(0f);

if (Input.GetKeyDown(KeyCode.F3))
{
    localPlayer.ProceduralWeaponAnimation.HandsContainer.HandsRotation.InputIntensity = 0;
    localPlayer.ProceduralWeaponAnimation.HandsContainer.HandsPosition.InputIntensity = 0;
    localPlayer.ProceduralWeaponAnimation.HandsContainer.SwaySpring._dampingRatio = 1;
    localPlayer.ProceduralWeaponAnimation.Breath.Intensity = 0;
    localPlayer.ProceduralWeaponAnimation.HandShakeEffector._shakeIntensity = 0;
    localPlayer.ProceduralWeaponAnimation.MotionReact.Intensity = 0;
    localPlayer.ProceduralWeaponAnimation.Breath.HipPenalty = 0;
    localPlayer.ProceduralWeaponAnimation.Walk.Intensity = 0;
    localPlayer.ProceduralWeaponAnimation.Walk.Overweight = 0;

    EFTHardSettings.Instance.SWAY_STRENGTH_PER_KG = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1000, 0));
    EFTHardSettings.Instance.DISPLACEMENT_STRENGTH_PER_KG = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1000, 0));
    
    Traverse.Create(localPlayer.ProceduralWeaponAnimation).Field("_swayStrength").SetValue(0f);
    Traverse.Create(localPlayer.ProceduralWeaponAnimation).Field("_displacementStr").SetValue(0f);
}
```