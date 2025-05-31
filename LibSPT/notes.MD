# Camera Confinement Handling
## Algorithm
* Run the advection until one of the following is met:
  1. The average absolute gradient is lower than some epsilon
  2. The magnitude of the advection vector is lower than some epsilon
  3. We exhaust the iteration limit
* Then do a final spherecast at that point and relocate the camera along the ray as far back as collisions allow (all the way if no collisions).

# !!!! TODO !!!!
Adjust the gradient calculation for the aim target:
1. Currently we calculate the gradient based on how far we can travel from the aim point towards the camera desired position. This\
   is not good, because small bumps at the far end can cause this number to suddenly be reduce to minimum.
2. Instead, we must maximize how far we can travel from the camera to the aim target. This means we'll apply a strong advection if a nearby wall is
   blocking the view, and zero to no advection if a far wall is blocking it.

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