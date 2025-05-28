# Camera Confinement Handling
* Don't do the complex visibility checking because heuristics will likely result in a twitchy and annoying camera. Let the user
  deal with swapping to the required position.
* We'll have two mechanisms to deal with confinement:
  * Do a bunch of short range raycasts from the first person camera position to determine confinement.
    * Ignore the player/hitcollider layer.
    * Spherecast 0.5m diameter backwards, right, left and upwards as far out as the magnitude of the Main Offset.
      * Calculate the mean confinement score as the average collision point distance and LERP between
        the Main and Shoulder cam positions between the range of 1/3 and 2/3 of the Main Offset magnitude (0.5m and 1m by def).
    * Spherecast 0.25m spheres 45 deg up, right, left and down, and raycast forward. Calculate confinement score based on a 2m max.
      * LERP as above.
    * This determines the desired position.
  * Spherecast 0.25m sphere from the base (first person) camera position to the desired position.
    * Get the closest point on that vector to any collision. If no collision, then we can be at the desired position.


# TODO
 - Look at GClass3407.method_1, this seems to take the current camera position, take the Player.CameraPosition as target, and tries
   to do collision checks (badly).

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