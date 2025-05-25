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
